using OpenTelemetry.Resources;
using OpenTelemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using pandapache.src.LoggingAndMonitoring;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using pandapache.src.Configuration;
using OpenTelemetry.Trace;
using PandApache3.src.Module;
using System.Collections.Concurrent;

namespace PandApache3.src.LoggingAndMonitoring
{
    public class Telemetry
    {

        private readonly Meter Meter = new("MyAppMeter", "1.0.0");
        private NetworkInterface _nic;
        public readonly Dictionary<string, Queue<KeyValuePair<DateTime, double>>> _metrics = new Dictionary<string, Queue<KeyValuePair<DateTime, double>>>();
        
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
            InitializeTelemetry();
            InitializeNetworkInterface();
            InitializeMetrics();
        }

        private void InitializeTelemetry()
        {
            Sdk.CreateMeterProviderBuilder()
                .AddMeter("MyAppMeter")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("PandApache3", serviceVersion: "3.4.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["environment"] = "dev"
                    })
                )
                .AddRuntimeInstrumentation()
                .Build();
        }

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

        private void InitializeMetrics()
        {
            var requiredKeys = new List<string>
        {
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

            foreach (var key in requiredKeys)
            {
                if (!_metrics.ContainsKey(key))
                {
                    _metrics[key] = new Queue<KeyValuePair<DateTime, double>>();
                }
            }
        }


        private ConcurrentDictionary<string, List<double>> InitializeAverages()
        {
            ConcurrentDictionary<string, List<double>> averages = new ConcurrentDictionary<string, List<double>>();
            foreach (var key in _metrics.Keys)
            {
                averages[key] = new List<double>();
            }
            return averages;
        }

        public async Task CollectMetricsAsync( int collectionDurationSeconds)
        {
            var averages = InitializeAverages();

            if (_metrics.First().Value.Count >  10)
            {
                Logger.LogDebug($"Limit of the metricSize reached");
                foreach (var key in _metrics.Keys)
                {
                    Logger.LogDebug($"Metric: {key}, size: {_metrics[key].Count}");
                    _metrics[key].Dequeue();
                }
            }

            int samples = 5;
            int delay = 600;

            int threadAvailable = ServerConfiguration.Instance.TelemetryThreadNumber;
            if (threadAvailable  < 3  )
            {
                samples = 2;
                delay = 200;
            }
            else if (threadAvailable < 6)
            {
                samples = 3;
                delay = 400;
            }

            Logger.LogDebug($"Telemetry is based on {samples} sample with {delay} ms between each");

            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < collectionDurationSeconds)
            {


                // Assuming you have a method to wait for the tasks to complete
                var tasks = new List<Task>
                {
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["CpuUsagePercentage"].Add(GetTelemetryValue("CpuUsagePercentage", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["AvailableMemoryMB"].Add(GetTelemetryValue("AvailableMemoryMB", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["PrivateMemoryUsageMB"].Add(GetTelemetryValue("PrivateMemoryUsageMB", samples, delay, convertToMB: true))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["VirtualMemoryUsageMB"].Add(GetTelemetryValue("VirtualMemoryUsageMB", samples, delay, convertToMB: true))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["DiskReadBytesPerSecond"].Add(GetTelemetryValue("DiskReadBytesPerSecond", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["DiskWriteBytesPerSecond"].Add(GetTelemetryValue("DiskWriteBytesPerSecond", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["DiskQueueLength"].Add(GetTelemetryValue("DiskQueueLength", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["GCCollectionCount"].Add(GetTelemetryValue("GCCollectionCount", samples, delay))),
                    Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["GCHeapSizeBytes"].Add(GetTelemetryValue("GCHeapSizeBytes", samples, delay))),
                };

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }
            Logger.LogDebug($"New collect start time: {startTime}");

            DateTime metricTimestamp = DateTime.Now;
            foreach (var average in averages)
            {
                _metrics[average.Key].Enqueue(new KeyValuePair<DateTime, double>(metricTimestamp, average.Value.Average()));
            }
        }

        private void PrintMetrics()
        {
            foreach (var metric in _metrics)
            {
                Logger.LogDebug($"Metric: {metric.Key}");
                foreach (var value in metric.Value)
                {
                    Logger.LogDebug($"\tTime: {value.Key}, Value: {value.Value}");
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
            Logger.LogDebug($"{metricName}: {averageValue}");
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
                Logger.LogDebug($" - {instance}");
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

            Logger.LogDebug($"Counters in category '{categoryName}':");
            foreach (PerformanceCounter counter in counters)
            {
                Logger.LogDebug($" - {counter.CounterName}");
            }
        }


    }
}
