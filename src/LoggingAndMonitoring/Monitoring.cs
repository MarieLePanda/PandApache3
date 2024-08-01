using pandapache.src.Configuration;
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
        public static string ActiveConnection()
        {
            string cmd = "Get-NetTCPConnection " +
                "| Where-Object { $_.State -eq 'Established' -and ($_.RemotePort -eq " + ServerConfiguration.Instance.ServerPort + " -or $_.RemotePort -eq " + ServerConfiguration.Instance.ServerPort + " ) } " +
                "| Select-Object LocalAddress, LocalPort, RemoteAddress, RemotePort";

            return RunPowerShelCommand(cmd);
        }

        public static string CurrentProcess()
        {
            string cmd = $"Get-Process -Name {Server.PROCESSNAME}";

            return RunPowerShelCommand(cmd);
        }
        public static string AllProcess()
        {
            string cmd = "Get-Process | Select-Object Name, CPU, ID, StartTime";

            return RunPowerShelCommand(cmd);
        }

        public static string Network()
        {
            string cmd = "Get-NetAdapter | Select-Object Name, Status, LinkSpeed";

            return RunPowerShelCommand(cmd);
        }

        public static string IOs()
        {
            StringBuilder result = new StringBuilder();
            List<string> cmds = new List<string>();
            cmds.Add("Get-WmiObject -Class Win32_PerfFormattedData_PerfDisk_LogicalDisk | Select-Object Name, DiskReadsPerSec, DiskWritesPerSec");
            cmds.Add("Get-WmiObject -Class Win32_PerfFormattedData_PerfDisk_LogicalDisk | Select-Object Name, AvgDiskSecPerRead, AvgDiskSecPerWrite");
            cmds.Add("Get-WmiObject -Class Win32_PerfFormattedData_PerfDisk_LogicalDisk | Select-Object Name, DiskBytesPerSec");
            cmds.Add("Get-WmiObject -Class Win32_PerfFormattedData_PerfDisk_LogicalDisk | Select-Object Name, CurrentDiskQueueLength");
            cmds.Add("Get-Process -Name " + Server.PROCESSNAME + " | Select-Object Name, Id, @{Name='IO Read Operations';Expression={$_.IOReadOperations}}, @{Name='IO Write Operations';Expression={$_.IOWriteOperations}}");
            cmds.Add("Get-WmiObject -Class Win32_PerfFormattedData_PerfDisk_LogicalDisk | Select-Object Name, CurrentDiskQueueLength");

            foreach (string cmd in cmds)
            {
                result.AppendLine(RunPowerShelCommand(cmd));
            }



            return result.ToString();
        }

        public static string Drive()
        {
            string cmd = "Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{Name='Used (GB)';Expression={[math]::round($_.Used/1GB,2)}}, @{Name='Free (GB)';Expression={[math]::round($_.Free/1GB,2)}}";

            return RunPowerShelCommand(cmd);
        }

        //Test function

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

        public static string GetCPU()
        {
            // Définir le script PowerShell
            string script = @"
            $cpuUsage = Get-WmiObject -Class Win32_PerfFormattedData_PerfOS_Processor | Where-Object { $_.Name -eq '_Total' }
            [PSCustomObject]@{
                InstanceName = $cpuUsage.Name
                PercentProcessorTime = $cpuUsage.PercentProcessorTime
            }";

            // Échapper les guillemets dans le script
            string escapedScript = script.Replace("\"", "\\\"").Replace("\n", "`n").Replace("\r", "`r");

            // Définir les options du processus
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{escapedScript}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                // Démarrer le processus PowerShell
                using (Process process = Process.Start(psi))
                {
                    using (System.IO.StreamReader reader = process.StandardOutput)
                    {
                        // Lire et afficher la sortie
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            string scriptCPU = "Get-Counter '\\Processor(_Total)\\% Processor Time'";
            return RunPowerShelCommand(scriptCPU);
        }

        private static string RunPowerShelCommand(string cmdlet)
        {
            string result = string.Empty;
            Logger.LogInfo($"Command executed on the server: {cmdlet}");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = cmdlet,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    using (System.IO.StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }

            Logger.LogInfo(result);
            return result;
        }
    }
}
