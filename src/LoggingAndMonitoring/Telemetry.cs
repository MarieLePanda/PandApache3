using OpenTelemetry.Resources;
using OpenTelemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using pandapache.src.LoggingAndMonitoring;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using pandapache.src.Configuration;

namespace PandApache3.src.LoggingAndMonitoring
{
    public class Telemetry
    {

        private readonly Meter Meter = new("MyAppMeter", "1.0.0");
        private readonly NetworkInterface _nic;        

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


        public double GetCpuUsagePercentage(int samples = 5, int delay = 500) => GetTelemetryValue("CpuUsagePercentage", samples, delay);
        public double GetAvailableMemoryMB() => GetTelemetryValue("AvailableMemoryMB");
        public double GetPrivateMemoryUsageMB() => GetTelemetryValue("PrivateMemoryUsageMB", convertToMB: true);
        public double GetVirtualMemoryUsageMB() => GetTelemetryValue("VirtualMemoryUsageMB", convertToMB: true);
        public double GetProcessCpuTime() => GetTelemetryValue("ProcessCpuTime");
        public double GetDiskReadBytesPerSecond() => GetTelemetryValue("DiskReadBytesPerSecond");
        public double GetDiskWriteBytesPerSecond() => GetTelemetryValue("DiskWriteBytesPerSecond");
        public double GetDiskQueueLength() => GetTelemetryValue("DiskQueueLength");
        public double GetNetworkBytesReceivedPerSecond() => GetTelemetryValue("NetworkBytesReceivedPerSecond");
        public double GetNetworkBytesSentPerSecond() => GetTelemetryValue("NetworkBytesSentPerSecond");
        public double GetGCCollectionCount() => GetTelemetryValue("GCCollectionCount");
        public double GetGCHeapSizeBytes() => GetTelemetryValue("GCHeapSizeBytes");

        public Telemetry()
        {
            Sdk.CreateMeterProviderBuilder()
                .AddMeter("MyAppMeter")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("PandApache3", serviceVersion: "3.4.0")
                    .AddAttributes(new Dictionary<string, object>
                                    {
                                        ["environment"] = "dev"
                                    }
                    )
                )
                .AddRuntimeInstrumentation()
                .Build();

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.Equals(ServerConfiguration.Instance.ServerIP))
                    {
                        _nic = nic;
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
            Logger.LogInfo($"{metricName}: {averageValue}");
            return averageValue;
        }

        public void ListInstances(string categoryName)
        {
            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                Logger.LogError($"Category '{categoryName}' does not exist.");
                return;
            }

            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            string[] instances = category.GetInstanceNames();

            Console.WriteLine($"Instances in category '{categoryName}':");
            foreach (string instance in instances)
            {
                Logger.LogInfo($" - {instance}");
            }
        }

        public  void ListCounters(string categoryName)
        {
            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                Logger.LogError($"Category '{categoryName}' does not exist.");
                return;
            }

            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            PerformanceCounter[] counters = category.GetCounters("_Total");

            Console.WriteLine($"Counters in category '{categoryName}':");
            foreach (PerformanceCounter counter in counters)
            {
                Logger.LogInfo($" - {counter.CounterName}");
            }
        }


    }
}
