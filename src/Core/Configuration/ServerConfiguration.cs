using System.Net;
using Newtonsoft.Json;
using PandApache3.src.Core.LoggingAndMonitoring;
using PandApache3.src.Core.Module;

namespace PandApache3.src.Core.Configuration
{
    public class ServerConfiguration
    {
        public ILogger ConfigLogger { get; set; }
        private FileSystemWatcher fileWatcher;
        private static ServerConfiguration instance;
        private static readonly object lockObject = new object();

        public string _configurationPath { get; set; }

        //General configuration
        public string ServerName { get; set; } = "PandApache3";

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress ServerIP { get; set; } = IPAddress.Any;
        public int ServerPort { get; set; } = 8080;
        public int AdminPort { get; set; } = 4040;

        //Performance
        public int MaxAllowedConnections { get; set; } = 100;
        public int MaxRejectedConnections { get; set; } = 50;
        public int TelemetryThreadNumber { get; set; } = 2;
        public int WebThreadNumber { get; set; } = 4;
        public int AdminThreadNumber { get; set; } = 2;


        //Logging
        public bool LogToFile { get; set; } = true;
        public bool LogToConsole { get; set; } = false;

        public string LogFolder { get; set; }
        public string LogFile { get; set; } = "PandApache3.log";
        public int MaxLogFile { get; set; } = 10;
        public int SizeLogFile { get; set; } = 1024;
        public int MaxBufferLog { get; set; } = 0;
        public int MaxHistoryLog { get; set; } = 10;

        public string LogLevel { get; set; } = "info";
        //Routing
        public string RootDirectory { get; set; }
        public string DocumentDirectory { get; set; }
        public string Persistence { get; set; } = "disk";

        //Security
        public bool AllowUpload { get; set; } = false;
        public bool AdminScript { get; set; } = false;
        public DirectoryConfig AdminDirectory { get; set; }
        //Other
        public string Platform { get; set; }
        public List<DirectoryConfig> Directories { get; set; } = new List<DirectoryConfig>();
        public List<ModuleConfiguration> Modules { get; set; } = new List<ModuleConfiguration>();

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


                            instance.ReloadConfiguration();
                            instance.ConfigLogger = new VirtualLogger("Configuration");

                        }
                    }
                }
                return instance;
            }
        }

        private ServerConfiguration()
        {
            ConfigLogger = Logger.Instance;
            //To fix
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
            Directories.Clear();
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
                ModuleConfiguration currentModule = null;
                ConfigLogger.LogDebug("Reading the configuration file line by line");
                foreach (var line in File.ReadLines(fullPath))
                {
                    // Ignorer les lignes vides et les commentaires
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    if (line.Trim().StartsWith("<") && line.Trim().EndsWith(">") && line.Trim().StartsWith("</") == false)
                    {
                        ConfigLogger.LogDebug($"Starting to read new directive {line.Trim()}");
                        string sectionName = line.Trim().Substring(1, line.Trim().Length - 2);
                        ConfigLogger.LogInfo($"Reading section {sectionName}");

                        if ((sectionName.StartsWith("Directory") || sectionName.StartsWith("Admin")) && currentDirectory == null)
                        {
                            string type = string.Empty;
                            if (sectionName.StartsWith("Admin"))
                                type = "admin";
                            else
                                type = "directory";

                            currentSection.Add(type);

                            ConfigLogger.LogDebug($"Section name: {sectionName}");
                            ConfigLogger.LogDebug($"sectionName.Split(' ')[0]: {sectionName.Split(' ')[0]}");
                            ConfigLogger.LogDebug($"sectionName.Split(' ')[1]: {sectionName.Split(' ')[1]}");

                            currentDirectory = new DirectoryConfig
                            {
                                Type = type,
                                Path = sectionName.Split(' ')[1]
                            };
                            Directories.Add(currentDirectory);
                            ConfigLogger.LogDebug($"Directories: {Directories}");

                            if (sectionName.StartsWith("Admin"))
                                currentSection.Add("Admin");
                            else
                                currentSection.Add("Directory");

                            ConfigLogger.LogDebug($"Section added: {sectionName}");
                        }
                        else if (sectionName.StartsWith("LimitVerb") && currentDirectory.AllowedMethods == null)
                        {
                            //allowedMethods.Clear();
                            currentSection.Add("LimitVerb");

                            currentDirectory.AllowedMethods = new List<string>();
                            ConfigLogger.LogDebug($"Section added: {sectionName}");
                            continue;
                        }
                        else if (sectionName.StartsWith("Module") && currentModule == null)
                        {
                            currentSection.Add("Module");
                            string moduleName = sectionName.Split(' ')[1];

                            currentModule = new ModuleConfiguration(moduleName);
                            Modules.Add(currentModule);

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
                            ConfigLogger.LogDebug($"Section end: Directory");

                            continue;
                        }
                        else if (line.Trim() == "</Admin>")
                        {
                            currentDirectory = null;
                            currentSection.Remove("Admin");
                            ConfigLogger.LogDebug($"Section end: Admin");

                            continue;
                        }
                        else if (line.Trim() == "</Module>")
                        {
                            currentModule = null;
                            currentSection.Remove("Module");
                            ConfigLogger.LogDebug($"Section end: Module");

                            continue;
                        }

                        else if (currentDirectory != null && currentSection.Last().Equals("Directory") || currentSection.Last().Equals("Admin") || currentSection.Last().Equals("Module"))
                        {
                            getKeyValue(line);
                        }
                        else if (currentDirectory != null && line.Trim() == "</LimitVerb>")
                        {
                            currentSection.Remove("LimitVerb");
                            ConfigLogger.LogDebug($"Section end: LimitVerb");

                            //currentDirectory.AllowedMethods = allowedMethods;
                            //allowedMethods = new List<string>();
                            //currentDirectory.AllowedMethods = new List<string>();
                            continue;
                        }
                        else if (currentDirectory != null && currentSection.Last().Equals("LimitVerb"))
                        {
                            currentDirectory.AllowedMethods.Add(line.Trim());
                            ConfigLogger.LogDebug($"Allow method: {line.Trim()}");

                        }


                    }
                    else
                    {
                        getKeyValue(line);
                    }

                }

                LoadAdminDirectory();

                ConfigLogger.LogInfo("Configuration reloaded");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during configuration reload: {ex.Message}");
            }


        }

        public DirectoryConfig? GetDirectory(string fullPath)
        {
            if (fullPath.StartsWith(AdminDirectory.URL))
            {
                ConfigLogger.LogDebug($"FilePath: {fullPath}");
                ConfigLogger.LogDebug($"AdminDirectoryPath:{AdminDirectory.Path}");
                return AdminDirectory;

            }
            foreach (DirectoryConfig directory in Directories)
            {
                ConfigLogger.LogDebug($"FilePath: {fullPath}");
                ConfigLogger.LogDebug($"DirectoryPath:{directory.Path}");
                if (fullPath.StartsWith(directory.Path, StringComparison.OrdinalIgnoreCase))
                {
                    return directory;
                }
            }

            return null;
        }

        private void LoadAdminDirectory()
        {
            foreach (DirectoryConfig directory in Directories)
            {
                if (directory.Type.Equals("admin"))
                {
                    AdminDirectory = directory;
                    AdminDirectory.Path = directory.Path;
                    AdminDirectory.URL = "/" + Path.GetFileName(Path.GetDirectoryName(directory.Path));
                    ConfigLogger.LogInfo($"Admin directory: {AdminDirectory.Path}");
                    return;
                }

            }
        }

        public void MapConfiguration(string key, string value)
        {
            var actionMap = new Dictionary<string, Action<string>>
            {
                ["servername"] = v => ServerName = v,
                ["serverip"] = v =>
                {
                    if (IPAddress.TryParse(v, out var parsedIPAddress))
                        ServerIP = parsedIPAddress;
                    else
                        ConfigLogger.LogWarning("Server IP invalid");
                },
                ["serverport"] = v => TrySetIntValue(v, val => ServerPort = val, "Server port invalid"),
                ["adminport"] = v => TrySetIntValue(v, val => AdminPort = val, "Admin port invalid"),
                ["maxallowedconnections"] = v => TrySetIntValue(v, val => MaxAllowedConnections = val, "Maximum allowed connection invalid"),
                ["maxrejectedconnections"] = v => TrySetIntValue(v, val => MaxRejectedConnections = val, "Maximum rejected connection invalid"),
                ["telemetrythreadnumber"] = v => TrySetIntValue(v, val => TelemetryThreadNumber = val, "Telemetry thread number invalid"),
                ["webthreadnumber"] = v => TrySetIntValue(v, val => WebThreadNumber = val, "Web thread number invalid"),
                ["adminthreadnumber"] = v => TrySetIntValue(v, val => AdminThreadNumber = val, "Admin thread number invalid"),
                ["logtofile"] = v => TrySetBoolValue(v, val => LogToFile = val, "LogToFile invalid"),
                ["logtoconsole"] = v => TrySetBoolValue(v, val => LogToConsole = val, "LogToConsole invalid"),
                ["logfolder"] = v => LogFolder = v,
                ["logfile"] = v => LogFile = v,
                ["maxlogfile"] = v => TrySetIntValue(v, val => MaxLogFile = val, "Maximum log file invalid"),
                ["sizelogfile"] = v => TrySetIntValue(v, val => SizeLogFile = val * 1024, "Size log file invalid"),
                ["maxbufferlog"] = v => TrySetIntValue(v, val => MaxBufferLog = val, "Maximum buffer size invalid"),
                ["maxhistorylog"] = v => TrySetIntValue(v, val => MaxHistoryLog = val, "Maximum hisotry log size invalid"),
                ["loglevel"] = v => LogLevel = v.ToLower(),
                ["rootdirectory"] = v => RootDirectory = v,
                ["documentdirectory"] = v => DocumentDirectory = v,
                ["allowupload"] = v => TrySetBoolValue(v, val => AllowUpload = val, "Allow upload invalid"),
                ["adminscript"] = v => TrySetBoolValue(v, val => AdminScript = val, "Admin script invalid"),
                ["persistence"] = v => Persistence = v.ToLower(),
                ["authtype"] = v => Directories.Last().AuthType = v,
                ["authname"] = v => Directories.Last().AuthName = v,
                ["authuserfile"] = v => Directories.Last().AuthUserFile = v,
                ["require"] = v => Directories.Last().Require = v,
                ["enable"] = v => TrySetBoolValue(v, val => Modules.Last().isEnable = val, "enable value invalid"),
                ["moduleloglevel"] = v => Modules.Last().Logger.LogLevel = v,
                ["modulelogfile"] = v => Modules.Last().Logger.LogFile = v


            };

            if (actionMap.TryGetValue(key.ToLower(), out var action))
            {
                action(value);
            }
            else
            {
                ConfigLogger.LogWarning($"Unknown configuration key: {key}");
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
                ConfigLogger.LogWarning(warningMessage);
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
                ConfigLogger.LogWarning(warningMessage);
            }
        }

        private void getKeyValue(string line)
        {
            ConfigLogger.LogDebug($"Key Value: {line}");

            var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                MapConfiguration(key, value);
            }
        }


        public string ExportJSON()
        {
            string json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            return json;

        }
        public void Export(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // General configuration
                writer.WriteLine("#General configuration");

                writer.WriteLine($"ServerName {ServerName}");
                writer.WriteLine($"ServerIP {ServerIP}");
                writer.WriteLine($"ServerPort {ServerPort}");

                // Performance
                writer.WriteLine("#Performance");

                writer.WriteLine($"MaxAllowedConnections {MaxAllowedConnections}");
                writer.WriteLine($"MaxRejectedConnections {MaxRejectedConnections}");

                // Logging
                writer.WriteLine("#Logging");

                writer.WriteLine($"LogToFile {LogToFile}");
                writer.WriteLine($"LogToConsole {LogToConsole}");
                writer.WriteLine($"LogFolder {LogFolder}");
                writer.WriteLine($"LogFile {LogFile}");
                writer.WriteLine($"MaxLogFile {MaxLogFile}");
                writer.WriteLine($"SizeLogFile {SizeLogFile}");
                writer.WriteLine($"LogLevel {LogLevel}");

                // Routing
                writer.WriteLine("#Routing");

                writer.WriteLine($"RootDirectory {RootDirectory}");
                writer.WriteLine($"DocumentDirectory {DocumentDirectory}");
                writer.WriteLine($"Persistence {Persistence}");

                // Security
                writer.WriteLine("#Security");

                writer.WriteLine($"AllowUpload {AllowUpload.ToString()}");

                foreach (var dir in Directories)
                {
                    if (dir.Type.Equals("admin"))
                    {
                        writer.WriteLine($"<Admin {Instance.AdminDirectory.Path.Substring(1)}>");
                        if (!string.IsNullOrEmpty(dir.AuthType))
                            writer.WriteLine($"\tAuthType {dir.AuthType}");
                        if (!string.IsNullOrEmpty(dir.AuthName))
                            writer.WriteLine($"\tAuthName {dir.AuthName}");
                        if (!string.IsNullOrEmpty(dir.AuthUserFile))
                            writer.WriteLine($"\tAuthUserFile {dir.AuthUserFile}");
                        writer.WriteLine($"\tRequire {dir.Require}");

                        writer.WriteLine($"\t<LimitVerb>");

                        foreach (var verb in dir.AllowedMethods)
                            writer.WriteLine($"\t\t{verb}");

                        writer.WriteLine($"\t<LimitVerb>");
                        writer.WriteLine("</Admin>");

                    }
                }
                // Other
                writer.WriteLine("#Other");

                // Export Directory configurations
                foreach (var dir in Directories)
                {
                    if (dir.Type.Equals("directory"))
                    {
                        writer.WriteLine($"<Directory {dir.Path}>");
                        if (!string.IsNullOrEmpty(dir.AuthType))
                            writer.WriteLine($"\tAuthType {dir.AuthType}");
                        if (!string.IsNullOrEmpty(dir.AuthName))
                            writer.WriteLine($"\tAuthName {dir.AuthName}");
                        if (!string.IsNullOrEmpty(dir.AuthUserFile))
                            writer.WriteLine($"\tAuthUserFile {dir.AuthUserFile}");
                        writer.WriteLine($"\tRequire {dir.Require}");

                        writer.WriteLine($"\t<LimitVerb>");

                        foreach (var verb in dir.AllowedMethods)
                            writer.WriteLine($"\t\t{verb}");

                        writer.WriteLine($"\t<LimitVerb>");
                        writer.WriteLine("</Directory>");

                    }
                }
            }
        }
    }

}
