using PandApache3.src.Core.Configuration;
using System.Diagnostics;
using System.Net.NetworkInformation;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Modules.Telemetry
{
    public class MetricCollectorWin : IMetricCollector
    {
        private NetworkInterface _nic;
        public List<string> MetricKeys()
        {
            return new List<string>{
                "CpuUsagePercentage",
                "AvailableMemoryMB",
                "PrivateMemoryUsageMB",
                "VirtualMemoryUsageMB",
                "DiskReadBytesPerSecond",
                "DiskWriteBytesPerSecond",
                "DiskQueueLength",
                "GCCollectionCount",
                "GCHeapSizeBytes"
            };
        }

        private readonly Dictionary<string, (string category, string counter, string instance)> staticCounters = new()
    {
        { "CpuUsagePercentage", ("Processor", "% Processor Time", "_Total") },
        { "AvailableMemoryMB", ("Memory", "Available MBytes", "") },
        { "DiskReadBytesPerSecond", ("PhysicalDisk", "Disk Read Bytes/sec", "_Total") },
        { "DiskWriteBytesPerSecond", ("PhysicalDisk", "Disk Write Bytes/sec", "_Total") },
        { "DiskQueueLength", ("PhysicalDisk", "Avg. Disk Queue Length", "_Total") },
        { "GCCollectionCount", (".NET CLR Memory", "# Gen 0 Collections", "_Global_") },
        { "GCHeapSizeBytes", (".NET CLR Memory", "Gen 0 Heap Size", "_Global_") }
    };

        private readonly Dictionary<string, (string category, string counter)> processSpecificCounters = new()
    {
        { "PrivateMemoryUsageMB", ("Process", "Private Bytes") },
        { "VirtualMemoryUsageMB", ("Process", "Virtual Bytes") },
        { "ProcessCpuTime", ("Process", "Process Time") }
    };

        private readonly Dictionary<string, (string category, string counter)> networkSpecificCounters = new()
    {
        { "NetworkBytesReceivedPerSecond", ("Network Interface", "Bytes Received/sec") },
        { "NetworkBytesSentPerSecond", ("Network Interface", "Bytes Sent/sec") }
    };

        private void InitializeNetworkInterface()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.Equals(ServerConfiguration.Instance.ServerIP))
                    {
                        _nic = nic;
                        return;
                    }
                }
            }
        }
        public double GetTelemetryValue(string metricName, int samples = 4, int delay = 1000, bool convertToMB = false)
        {
            string category, counter, instance;

            if (staticCounters.TryGetValue(metricName, out var staticCounterInfo))
            {
                (category, counter, instance) = staticCounterInfo;
            }

            else if (processSpecificCounters.TryGetValue(metricName, out var processSpecificCounterInfo))
            {
                (category, counter) = processSpecificCounterInfo;
                instance = Process.GetCurrentProcess().ProcessName;
            }
            else if (processSpecificCounters.TryGetValue(metricName, out var networkSpecificCounters))
            {
                (category, counter) = networkSpecificCounters;
                instance = _nic.Name;
            }

            else
            {
                throw new ArgumentException($"Unknown metric: {metricName}");
            }

            double totalValue = 0;
            using (var performanceCounter = new PerformanceCounter(category, counter, instance))
            {
                performanceCounter.NextValue(); // Initial call to start measurement
                for (int i = 0; i < samples; i++)
                {
                    Thread.Sleep(delay); // Wait between samples
                    double value = performanceCounter.NextValue();
                    totalValue += convertToMB ? value / (1024 * 1024) : value;
                }
            }

            double averageValue = totalValue / samples;
            ExecutionContext.Current.Logger.LogDebug($"{metricName}: {averageValue}");
            return averageValue;
        }
    }
}
