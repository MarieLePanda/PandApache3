using Newtonsoft.Json.Linq;
using PandApache3.src.Core;
using PandApache3.src.Core.Configuration;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Modules.Telemetry
{
    public class MetricCollectorUnix : IMetricCollector
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
            };
        }

        private readonly Dictionary<string, string> staticCountersLinux = new()
        {
            { "CpuUsagePercentage", "/proc/stat" },
            { "AvailableMemoryMB", "/proc/meminfo" },
            { "PrivateMemoryUsageMB", $"/proc/{Startup.PROCESSID}/status"},
            { "VirtualMemoryUsageMB", $"/proc/{Startup.PROCESSID}/status"},
            { "DiskReadBytesPerSecond", "/proc/diskstats"},
            { "DiskWriteBytesPerSecond", "/proc/diskstats" },
            { "DiskQueueLength", "/proc/diskstats" },
            { "NetworkBytesReceivedPerSecond", "/proc/net/dev" }, // Réseau : octets reçus
            { "NetworkBytesSentPerSecond", "/proc/net/dev" } // Réseau : octets envoyés
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
            double totalValue = 0;

            for(int i = 0; i < samples; i++)
            {
                Thread.Sleep(delay); // Wait between samples
                string content = string.Empty;
                if(!metricName.Equals("DiskReadBytesPerSecond") || metricName.Equals("DiskWriteBytesPerSecond"))
                    content = File.ReadAllText(staticCountersLinux[metricName]);
                double value = metricName switch
                {
                    "CpuUsagePercentage" => ParseCpuUsage(content),
                    "AvailableMemoryMB" => ParseAvailableMemory(content),
                    "PrivateMemoryUsageMB" => ParseStatus(content, "VmRSS"),
                    "VirtualMemoryUsageMB" => ParseStatus(content, "VmSize"),
                    "DiskReadBytesPerSecond" => ParseDiskstats(staticCountersLinux[metricName], 3),
                    "DiskWriteBytesPerSecond" => ParseDiskstats(staticCountersLinux[metricName], 7),
                    "DiskQueueLength" => ParseDiskstatsQueue(content, 11),
                    "NetworkBytesReceivedPerSecond" => ParseNetworkBytes(content, received: true),
                    "NetworkBytesSentPerSecond" => ParseNetworkBytes(content, received: false),
                    _ => throw new NotSupportedException($"Parser for metric '{metricName}' not implemented.")
                };
                totalValue += convertToMB ? value /  1024 : value;

                ExecutionContext.Current.Logger.LogDebug($"Telemetry value: {totalValue} for metric {metricName}");
            }

            double averageValue = totalValue / samples;
            ExecutionContext.Current.Logger.LogInfo($"{metricName}: {averageValue}");
            
            return averageValue;
        }

            private double ParseCpuUsage(string statContent)
            {
                var lines = statContent.Split('\n');
                var cpuLine = lines.FirstOrDefault(line => line.StartsWith("cpu ")); // Ligne globale CPU
                if (cpuLine == null) throw new InvalidOperationException("Could not find CPU stats.");

                var values = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(long.Parse).ToArray();
                long idleTime = values[3]; // idle
                long totalTime = values.Sum();

                // Calcul d'utilisation CPU (différence sur plusieurs échantillons pourrait être mieux)
                return 100.0 * (totalTime - idleTime) / totalTime;
            }

        private double ParseAvailableMemory(string memInfoContent)
        {
            var lines = memInfoContent.Split('\n');
            var memAvailableLine = lines.FirstOrDefault(line => line.StartsWith("MemAvailable:"));

            if (memAvailableLine != null)
            {
                var availableKb = long.Parse(memAvailableLine.Split(':')[1].Trim().Split(' ')[0]);
                return availableKb;
            }
            else
            {
                ExecutionContext.Current.Logger.LogError("Could not find MemAvailable in /proc/meminfo");
                return -1;
            }
        }

        private double ParseStatus(string statusContent, string name)
        {
            var lines = statusContent.Split('\n');
            var attribut = lines.FirstOrDefault(line => line.StartsWith(name));

            if (attribut != null)
            {
                var valueInKb = double.Parse(attribut.Split(':')[1].Trim().Split(' ')[0]);
                return valueInKb ;
            }
            else
            {
                ExecutionContext.Current.Logger.LogError($"Could not find {name} in /proc/{Startup.PROCESSID}/status");
                return -1;
            }
        }

        private double ParseDiskstats(string fileName, int column)
        {
            long[] sectorValue = new long[2];
            for(int i = 0; i < 2; i++)
            {
                string diskstatsContent = File.ReadAllText(fileName);
                Thread.Sleep(1000);
                var lines = diskstatsContent.Split('\n');
                var sdaLine = lines.FirstOrDefault(line =>
                {
                    var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return columns.Length > 2 && columns[2] == "sdc";
                });

                if (sdaLine != null)
                {
                    string[] sdaArray = sdaLine.Split(" ");
                    List<string> sdaCleaned = new List<string>();

                    foreach(string value in sdaArray)
                    {
                        if (!string.IsNullOrEmpty(value))
                            sdaCleaned.Add(value);
                    }
                    string toParse = sdaCleaned[column];
                    sectorValue[i] = long.Parse(toParse);
                }
                else
                {
                    ExecutionContext.Current.Logger.LogError("'sdc' not found in diskstats.");
                    return -1;
                }

            }

            long bytesPerSecond = (sectorValue[1] - sectorValue[0]) * 512;

            return bytesPerSecond;
        }

        private double ParseDiskstatsQueue(string diskContent, int column)
        {
            var lines = diskContent.Split('\n');

            var sdaLine = lines.FirstOrDefault(line =>
            {
                var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return columns.Length > 2 && columns[2] == "sdc";
            });
            if (sdaLine != null)
            {
                string[] sdaArray = sdaLine.Split(" ");
                List<string> sdaCleaned = new List<string>();

                foreach (string value in sdaArray)
                {
                    if (!string.IsNullOrEmpty(value))
                        sdaCleaned.Add(value);
                }
                string toParse = sdaCleaned[column];
                long queueLength = long.Parse(toParse);
                return queueLength;
            }
            else
            {
                ExecutionContext.Current.Logger.LogError("'sdc' not found in diskstats.");
                return -1;
            }

        }

        private double ParseNetworkBytes(string netDevContent, bool received)
        {
            var lines = netDevContent.Split('\n').Skip(2); // Sauter les deux premières lignes d'entêtes
            var totalBytes = 0L;

            foreach (var line in lines)
            {
                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0)
                {
                    totalBytes += received
                        ? long.Parse(tokens[1]) // Octets reçus
                        : long.Parse(tokens[9]); // Octets envoyés
                }
            }

            return totalBytes; // Conversion éventuelle en kB ou MB si nécessaire
        }

    }

}
