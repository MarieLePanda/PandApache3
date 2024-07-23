using pandapache.src.LoggingAndMonitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.LoggingAndMonitoring
{
    public class Monitoring
    {
        public static Dictionary<string, long> getProcessMemory()
        {
            string FreePhysicalMemory;
            string TotalVisibleMemorySize;
            Dictionary<string, long> memoryInformation = new Dictionary<string, long>();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "os get FreePhysicalMemory,TotalVisibleMemorySize /Format:List",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    using (System.IO.StreamReader reader = process.StandardOutput)
                    {
                        string output = reader.ReadToEnd();
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var keyValuePairs = new Dictionary<string, long>();

                        foreach (var line in lines)
                        {
                            var parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                if (long.TryParse(parts[1].Trim(), out long value))
                                {
                                    keyValuePairs[key] = value;
                                }
                                else
                                {
                                    Console.WriteLine($"Erreur de parsing de la valeur pour {key}: {parts[1].Trim()}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Ligne de sortie non valide : {line}");
                            }
                        }

                        if (keyValuePairs.TryGetValue("FreePhysicalMemory", out long freePhysicalMemory) &&
      keyValuePairs.TryGetValue("TotalVisibleMemorySize", out long totalVisibleMemorySize))
                        {
                            memoryInformation.Add("FreePhysicalMemory", freePhysicalMemory);
                            memoryInformation.Add("TotalVisibleMemorySize", totalVisibleMemorySize);
                            memoryInformation.Add("TotalUsedMemorySize", totalVisibleMemorySize - freePhysicalMemory);
                        }
                    }
                }
            }
            catch { }

            Logger.LogInfo($"Memory used: {memoryInformation["TotalUsedMemorySize"] / 1024 / 1024} Go");
            Logger.LogInfo($"Memory free: {memoryInformation["FreePhysicalMemory"] / 1024 / 1024} Go");
            Logger.LogInfo($"Memory total: {memoryInformation["TotalVisibleMemorySize"] / 1024 / 1024} Go");


            return memoryInformation;
        }

        public static long GetProcessGC()
        {
            long totalMemory = GC.GetTotalMemory(false);
            Logger.LogInfo($"total memory allocated (GC) : {totalMemory / 1024 / 1024} Mo");
            return totalMemory;

        }

        public static Dictionary<string, long> GetDriveInfo() 
        {
            Dictionary<string, long> driveInfo = new Dictionary<string, long>();
            DriveInfo drive = new DriveInfo("C");
            Logger.LogInfo($"Total space : {drive.TotalSize / 1024 / 1024 / 1024} Go");
            driveInfo.Add("TotalSize", drive.TotalSize);
            Logger.LogInfo($"Total available free space : {drive.AvailableFreeSpace / 1024 / 1024 / 1024} Go");
            driveInfo.Add("AvailableFreeSpace", drive.AvailableFreeSpace);
            Logger.LogInfo($"Total used space : {(drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024} Go");
            driveInfo.Add("UsedSpace", (drive.TotalSize - drive.AvailableFreeSpace));

            return driveInfo;

        }

        public static float GetCPU()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            // Attendre un court instant pour obtenir une mesure précise
            Thread.Sleep(1000);
            float cpuUsage = cpuCounter.NextValue();
            Console.WriteLine($"Utilisation du CPU : {cpuUsage}%");
            return cpuUsage;
        }
    }
}
