using OpenTelemetry.Resources;
using OpenTelemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Net.NetworkInformation;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;
using PandApache3.src.Core;
using PandApache3.src.Core.Module;
using PandApache3.src.Core.Configuration;

namespace PandApache3.src.Modules.Telemetry
{
    public class Telemetry
    {

        private readonly Meter Meter = new("MyAppMeter", "1.0.0");
        public readonly Dictionary<string, Queue<KeyValuePair<DateTime, double>>> _metrics = new Dictionary<string, Queue<KeyValuePair<DateTime, double>>>();
        private IMetricCollector _collector;


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

        

        private void InitializeMetrics()
        {
            if(ServerConfiguration.Instance.Platform.Equals("WIN"))
            {
                _collector = new MetricCollectorWin();
            }
            else
            {
                _collector = new MetricCollectorUnix();
            }
            var requiredKeys = _collector.MetricKeys();

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

        public async Task CollectMetricsAsync(int collectionDurationSeconds)
        {
            var averages = InitializeAverages();

            if (_metrics.First().Value.Count > 10)
            {
                ExecutionContext.Current.Logger.LogInfo($"Limit of the metricSize reached for the first one");
                foreach (var key in _metrics.Keys)
                {
                    ExecutionContext.Current.Logger.LogInfo($"Metric: {key}, size: {_metrics[key].Count}");
                    int limit = 0;
                    while(_metrics[key].Count > 10 || limit > 100)
                    {
                        _metrics[key].Dequeue();
                        limit++;
                    }
                    ExecutionContext.Current.Logger.LogInfo($"Metric: {key}, new size: {_metrics[key].Count}");

                }
            }

            int samples = 5;
            int delay = 600;

            int threadAvailable = ServerConfiguration.Instance.TelemetryThreadNumber;
            if (threadAvailable < 3)
            {
                samples = 2;
                delay = 200;
            }
            else if (threadAvailable < 6)
            {
                samples = 3;
                delay = 400;
            }

            ExecutionContext.Current.Logger.LogDebug($"Telemetry is based on {samples} sample with {delay} ms between each");

            DateTime startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalSeconds < collectionDurationSeconds)
            {
                List<Task> tasks;
                // Assuming you have a method to wait for the tasks to complete
                if(ServerConfiguration.Instance.Platform.Equals("WIN"))
                {
                    tasks = new List<Task>
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
                }
                else
                {
                    tasks = new List<Task>
                    {
                        //Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["CpuUsagePercentage"].Add(GetTelemetryValue("CpuUsagePercentage", samples, delay))),
                        //Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["AvailableMemoryMB"].Add(GetTelemetryValue("AvailableMemoryMB", samples, delay))),
                        Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry).TaskFactory.StartNew(() => averages["PrivateMemoryUsageMB"].Add(GetTelemetryValue("PrivateMemoryUsageMB", samples, delay, true))),

                    };
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }

            ExecutionContext.Current.Logger.LogDebug($"New collect start time: {startTime}");

            DateTime metricTimestamp = DateTime.Now;
            foreach (var average in averages)
            {
                if (average.Value.Count > 0)
                    _metrics[average.Key].Enqueue(new KeyValuePair<DateTime, double>(metricTimestamp, average.Value.Average()));
                else
                    ExecutionContext.Current.Logger.LogWarning($"Any telemetry value was collected for the metric {average.Key}");
            }
        }

        private void PrintMetrics()
        {
            foreach (var metric in _metrics)
            {
                ExecutionContext.Current.Logger.LogDebug($"Metric: {metric.Key}");
                foreach (var value in metric.Value)
                {
                    ExecutionContext.Current.Logger.LogDebug($"\tTime: {value.Key}, Value: {value.Value}");
                }
            }
        }

        public double GetTelemetryValue(string metricName, int samples = 4, int delay = 1000, bool convertToMB = false)
        {
            return _collector.GetTelemetryValue(metricName, samples, delay, convertToMB); 
        }

        public void ListInstances(string categoryName)
        {
            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                ExecutionContext.Current.Logger.LogError($"Category '{categoryName}' does not exist.");
                return;
            }

            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            string[] instances = category.GetInstanceNames();

            Console.WriteLine($"Instances in category '{categoryName}':");
            foreach (string instance in instances)
            {
                ExecutionContext.Current.Logger.LogDebug($" - {instance}");
            }
        }

        public void ListCounters(string categoryName)
        {
            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                ExecutionContext.Current.Logger.LogError($"Category '{categoryName}' does not exist.");
                return;
            }

            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            PerformanceCounter[] counters = category.GetCounters("_Total");

            ExecutionContext.Current.Logger.LogDebug($"Counters in category '{categoryName}':");
            foreach (PerformanceCounter counter in counters)
            {
                ExecutionContext.Current.Logger.LogDebug($" - {counter.CounterName}");
            }
        }


    }
}
