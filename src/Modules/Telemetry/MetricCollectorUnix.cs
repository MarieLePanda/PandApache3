using Newtonsoft.Json.Linq;
using PandApache3.src.Core;
using PandApache3.src.Core.Configuration;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.NetworkInformation;
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
                "PrivateMemoryUsageMB"
            };
        }

        private readonly Dictionary<string, string> staticCountersLinux = new()
        {
            { "CpuUsagePercentage", "/proc/stat" },
            { "AvailableMemoryMB", "/proc/meminfo" },
            { "PrivateMemoryUsageMB", $"/proc/{Startup.PROCESSID}/status"},
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
                string content = File.ReadAllText(staticCountersLinux[metricName]);
                double value = metricName switch
                {
                    "CpuUsagePercentage" => ParseCpuUsage(content),
                    "AvailableMemoryMB" => ParseAvailableMemory(content),
                    "PrivateMemoryUsageMB" => ParsePrivateMemoryUsage(content),
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

        private double ParsePrivateMemoryUsage(string statusContent)
        {
            var lines = statusContent.Split('\n');
            var VmRSS = lines.FirstOrDefault(line => line.StartsWith("VmRSS"));

            if (VmRSS != null)
            {
                var valueInKb = double.Parse(VmRSS.Split(':')[1].Trim().Split(' ')[0]);
                return valueInKb ;
            }
            else
            {
                ExecutionContext.Current.Logger.LogError($"Could not find VmRSS in /proc/{Startup.PROCESSID}/status");
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
