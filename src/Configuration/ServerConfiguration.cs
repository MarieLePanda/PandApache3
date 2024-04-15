using pandapache.src.LoggingAndMonitoring;
using System.Net;

namespace pandapache.src.Configuration
{
    public class ServerConfiguration : IServerConfiguration
    {
        private FileSystemWatcher fileWatcher;
        private string _configurationPath;
        private static ServerConfiguration instance;
        private static readonly object lockObject = new object();


        //General configuration
        public string ServerName { get; set; } = "PandApache3";
        public IPAddress ServerIP { get; set; } = System.Net.IPAddress.Any;
        public int ServerPort { get; set; } = 5000;

        //Performance
        public int MaxAllowedConnections { get; set; } = 100;
        public int MaxRejectedConnections { get; set; } = 50;

        //Logging
        public string LogFolder { get; set; }
        public string LogFile { get; set; } = "PandApache3.log";
        public int MaxLogFile { get; set; } = 10;
        public int SizeLogFile { get; set; } = 1024;
        public string LogLevel { get; set; } = "info";
        //Routing
        public string RootDirectory { get; set; }
        public string DocumentDirectory {  get; set; }
        public string Persistence { get; set; } = "disk";

        //Security
        public bool AllowUpload { get; set; } = false;
        //Other
        public string Platform{ get; set; }
        // Ajoutez d'autres propriétés de configuration selon vos besoins

        public static ServerConfiguration Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new ServerConfiguration();

                            OperatingSystem os = Environment.OSVersion;

                            if (os.Platform == PlatformID.Win32NT || os.Platform == PlatformID.Win32Windows)
                            {
                                instance.Platform = "WIN";
                                instance._configurationPath = @"C:\PandApache3\conf\";
                                instance.LogFolder = @"C:\PandApache3\log\";
                                instance.RootDirectory = @"C:\PandApache3\www\";
                                instance.DocumentDirectory = @"C:\PandApache3\documents\";

                            }
                            // Vérifie si le système d'exploitation est Linux
                            else if (os.Platform == PlatformID.Unix)
                            {
                                instance.Platform = "UNIX";
                                instance._configurationPath = @"/etc/PandApache3/conf/";
                                instance.LogFolder = @"/var/log/PandApache3/";
                                instance.RootDirectory = @"/etc/PandApache3/www/";
                                instance.DocumentDirectory = @"/etc/PandApache3/documents/";

                            }
                            else
                            {
                                throw new Exception("Operating system not supported");
                            }

                        }
                    }
                }
                return instance;
            }
        }

        // Private constructor to prevent instantiation
        private ServerConfiguration() 
        {
            if (File.Exists(_configurationPath))
            {
                // Initialisez FileSystemWatcher pour surveiller les changements dans le fichier de configuration
                fileWatcher = new FileSystemWatcher();
                fileWatcher.Path = _configurationPath;
                fileWatcher.Filter = "*.conf";
                fileWatcher.Changed += OnConfigurationFileChanged;
                fileWatcher.EnableRaisingEvents = true;
                ReloadConfiguration();

            }
        }

        private void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
        {
            // Traitez l'événement de changement du fichier de configuration
            // Par exemple, rechargez les paramètres de configuration depuis le fichier
            ReloadConfiguration();
        }

        public void ReloadConfiguration()
        {
            // Implémentez la logique pour recharger les paramètres de configuration depuis le fichier
            Logger.LogInfo("Load configuration");
            string fullPath = Path.Combine(_configurationPath, "PandApache3.conf");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The configuration file didn't exist", fullPath);
            }

            try
            {
                foreach (var line in File.ReadLines(fullPath))
                {
                    // Ignorer les lignes vides et les commentaires
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    // Diviser la ligne en clé et valeur en utilisant le premier espace trouvé
                    var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        MapConfiguration(key, value);
                    }
                }
                 Logger.LogInfo("Configuration reloaded");
            }
            catch (Exception ex)
            {
                throw new Exception("Error during configuration reload", ex);
            }


        }

        private void MapConfiguration(string key, string value)
        {
            var actionMap = new Dictionary<string, Action<string>>
            {
                ["servername"] = v => ServerName = v,
                ["serverip"] = v => {
                    if (IPAddress.TryParse(v, out var parsedIPAddress))
                        ServerIP = parsedIPAddress;
                    else
                        Logger.LogWarning("Server IP invalid");
                },
                ["serverport"] = v => TrySetIntValue(v, val => ServerPort = val, "Server port invalid"),
                ["maxallowedconnections"] = v => TrySetIntValue(v, val => MaxAllowedConnections = val, "Maximum allowed connection invalid"),
                ["maxrejectedconnections"] = v => TrySetIntValue(v, val => MaxRejectedConnections = val, "Maximum rejected connection invalid"),
                ["logfolder"] = v => LogFolder = v,
                ["logfile"] = v => LogFile = v,
                ["maxlogfile"] = v => TrySetIntValue(v, val => MaxLogFile = val, "Maximum log file invalid"),
                ["sizelogfile"] = v => TrySetIntValue(v, val => SizeLogFile = val * 1024, "Size log file invalid"),
                ["loglevel"] = v => LogLevel = v,
                ["rootdirectory"] = v => RootDirectory = v,
                ["documentdirectory"] = v => DocumentDirectory = v,
                ["allowupload"] = v => TrySetBoolValue(v, val => AllowUpload = val, "Allow upload invalid"),
                ["persistence"] = v => Persistence = v
            };

            if (actionMap.TryGetValue(key.ToLower(), out var action))
            {
                action(value);
            }
            else
            {
                Logger.LogWarning($"Unknown configuration key: {key}");
            }
        }

        private void TrySetIntValue(string value, Action<int> setAction, string warningMessage)
        {
            if (int.TryParse(value, out var parsedValue))
            {
                setAction(parsedValue);
            }
            else
            {
                Logger.LogWarning(warningMessage);
            }
        }

        private void TrySetBoolValue(string value, Action<bool> setAction, string warningMessage)
        {
            if (bool.TryParse(value, out var parsedValue))
            {
                setAction(parsedValue);
            }
            else
            {
                Logger.LogWarning(warningMessage);
            }
        }
    }

}
