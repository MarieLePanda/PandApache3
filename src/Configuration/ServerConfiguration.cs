using pandapache.src.LoggingAndMonitoring;
using PandApache3.src.Configuration;
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
        public bool LogToFile { get; set; } = true;
        public bool LogToConsole { get; set; } = false;

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
        public DirectoryConfig AdminDirectory {  get; set; }
        //Other
        public string Platform{ get; set; }
        public List<DirectoryConfig> Directories { get; set; } = new List<DirectoryConfig>();
        public string AuthName {  get; set; }
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

        private ServerConfiguration() 
        {
            if (File.Exists(_configurationPath))
            {
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
            ReloadConfiguration();
        }

        public void ReloadConfiguration()
        {
            string fullPath = Path.Combine(_configurationPath, "PandApache3.conf");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The configuration file didn't exist", fullPath);
            }

            try
            {
                List<string> allowedMethods = new List<string>();

                List<string> currentSection = new List<string>();
                DirectoryConfig currentDirectory = null;

                foreach (var line in File.ReadLines(fullPath))
                {
                    // Ignorer les lignes vides et les commentaires
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    if (line.Trim().StartsWith("<") && line.Trim().EndsWith(">") && line.Trim().StartsWith("</") == false)
                    {
                        Logger.LogDebug($"Starting to read new directive {line.Trim()}");
                        string sectionName = line.Trim().Substring(1, line.Trim().Length - 2);
                        Logger.LogInfo($"Reading section {sectionName}");

                        if ((sectionName.StartsWith("Directory") || sectionName.StartsWith("Admin")) && currentDirectory == null)
                        {
                            string type = string.Empty;
                            if (sectionName.StartsWith("Admin"))
                                type = "admin";
                            else
                                type = "directory";

                            currentSection.Add(type);

                            currentDirectory = new DirectoryConfig
                            {
                                Type = type,
                                Path = sectionName.Split(' ')[1]
                            };
                            Directories.Add(currentDirectory);
                            Logger.LogDebug($"Directories: {Directories}");

                            if(sectionName.StartsWith("Admin"))
                                currentSection.Add("Admin");
                            else
                                currentSection.Add("Directory");

                            Logger.LogDebug($"Section added: {sectionName}");
                        }
                        else if (sectionName.StartsWith("LimitVerb"))
                        {
                            allowedMethods.Clear();
                            currentSection.Add("LimitVerb");
                            
                            Logger.LogDebug($"Section added: {sectionName}");
                            continue;
                        }
                        continue;
                    }

                    if (currentSection.Count != 0)
                    {

                        if (line.Trim() == "</Directory>")
                        {
                            
                            currentDirectory = null;
                            currentSection.Remove("Directory");
                            Logger.LogDebug($"Section end: Directory");

                            continue;
                        }
                        else if (line.Trim() == "</Admin>")
                        {
                            currentDirectory = null;
                            currentSection.Remove("Admin");
                            Logger.LogDebug($"Section end: Admin");

                            continue;
                        }
                        else if (currentDirectory != null && currentSection.Last().Equals("Directory") || currentSection.Last().Equals("Admin"))
                        {
                            getKeyValue(line);
                        }
                        else if (currentDirectory != null && line.Trim() == "</LimitVerb>")
                        {
                            currentSection.Remove("LimitVerb");
                            Logger.LogDebug($"Section end: LimitVerb");

                            currentDirectory.AllowedMethods = allowedMethods;
                            allowedMethods = new List<string>();
                            continue;
                        }
                        else if(currentDirectory != null && currentSection.Last().Equals("LimitVerb"))
                        {
                            allowedMethods.Add(line.Trim());
                            Logger.LogDebug($"Allow method: {line.Trim()}");

                        }

                    }
                    else
                    {
                        getKeyValue(line);
                    }

                }

                LoadAdminDirectory();

                 Logger.LogInfo("Configuration reloaded");
            }
            catch (Exception ex)
            {
                throw new Exception("Error during configuration reload", ex);
            }


        }

        private void LoadAdminDirectory()
        {
            foreach(DirectoryConfig directory in Directories)
            {
                if(directory.Type.Equals("admin"))
                {
                    AdminDirectory = directory;
                    AdminDirectory.Path = "/" + directory.Path.Remove(0, RootDirectory.Length);
                    Logger.LogInfo($"Admin directory: {AdminDirectory.Path}");
                    return;
                }

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
                ["logtofile"] = v => TrySetBoolValue(v, val => LogToFile = val, "LogToFile invalid"),
                ["logtoconsole"] = v => TrySetBoolValue(v, val => LogToConsole = val, "LogToConsole invalid"),
                ["logfolder"] = v => LogFolder = v,
                ["logfile"] = v => LogFile = v,
                ["maxlogfile"] = v => TrySetIntValue(v, val => MaxLogFile = val, "Maximum log file invalid"),
                ["sizelogfile"] = v => TrySetIntValue(v, val => SizeLogFile = val * 1024, "Size log file invalid"),
                ["loglevel"] = v => LogLevel = v.ToLower(),
                ["rootdirectory"] = v => RootDirectory = v,
                ["documentdirectory"] = v => DocumentDirectory = v,
                ["allowupload"] = v => TrySetBoolValue(v, val => AllowUpload = val, "Allow upload invalid"),
                ["persistence"] = v => Persistence = v.ToLower(),
                ["authtype"] = v => Directories.Last().AuthType = v,
                ["authname"] = v => Directories.Last().AuthName = v,
                ["authuserfile"] = v => Directories.Last().AuthUserFile = v,
                ["require"] = v => Directories.Last().Require = v


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

        private void getKeyValue(string line)
        {
            Logger.LogDebug($"Key Value: {line}");

            var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                MapConfiguration(key, value);
            }
        }

        public DirectoryConfig? GetDirectory(string fullPath)
        {
            foreach (DirectoryConfig directory in Directories)
            {
                Logger.LogDebug($"FilePath: {fullPath}");
                Logger.LogDebug($"DirectoryPath:{directory.Path}");
                if (fullPath.StartsWith(directory.Path, StringComparison.OrdinalIgnoreCase))
                {
                    return directory;
                }
            }

            return null;
        }
        public void Export(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // General configuration
                writer.WriteLine($"ServerName {ServerName}");
                writer.WriteLine($"ServerIP {ServerIP}");
                writer.WriteLine($"ServerPort {ServerPort}");

                // Performance
                writer.WriteLine($"MaxAllowedConnections {MaxAllowedConnections}");
                writer.WriteLine($"MaxRejectedConnections {MaxRejectedConnections}");

                // Logging
                writer.WriteLine($"LogFolder {LogFolder}");
                writer.WriteLine($"LogFile {LogFile}");
                writer.WriteLine($"MaxLogFile {MaxLogFile}");
                writer.WriteLine($"SizeLogFile {SizeLogFile}");
                writer.WriteLine($"LogLevel {LogLevel}");

                // Routing
                writer.WriteLine($"RootDirectory {RootDirectory}");
                writer.WriteLine($"DocumentDirectory {DocumentDirectory}");
                writer.WriteLine($"Persistence {Persistence}");

                // Security
                writer.WriteLine($"AllowUpload {AllowUpload.ToString()}");

                // Other
                if (!string.IsNullOrEmpty(Platform))
                    writer.WriteLine($"Platform {Platform}");

                // Export Directory configurations
                foreach (var dir in Directories)
                {
                    writer.WriteLine("<Directory>");
                    writer.WriteLine($"Path {dir.Path}");
                    if (!string.IsNullOrEmpty(dir.AuthType))
                        writer.WriteLine($"AuthType {dir.AuthType}");
                    if (!string.IsNullOrEmpty(dir.AuthName))
                        writer.WriteLine($"AuthName {dir.AuthName}");
                    if (!string.IsNullOrEmpty(dir.AuthUserFile))
                        writer.WriteLine($"AuthUserFile {dir.AuthUserFile}");
                    writer.WriteLine($"RequireValidUser {dir.Require}");
                    
                    writer.WriteLine($"<LimitVerb>");

                    foreach(var verb in dir.AllowedMethods)
                        writer.WriteLine($"{verb}");

                    writer.WriteLine($"<LimitVerb>");
                    writer.WriteLine("</Directory>");
                }
            }
        }
    }

}
