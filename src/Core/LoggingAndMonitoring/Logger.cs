using PandApache3.src.Core.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PandApache3.src.Core.LoggingAndMonitoring
{
    public class Logger : ILogger
    {

        private string logDirectory;
        private string logFileName;
        private int maxLogFiles;
        private int maxSizeFile;
        //private string logLevel;
        private int maxBufferSize = 100;
        private ConcurrentQueue<LogEntry> logs = new ConcurrentQueue<LogEntry>();
        private SortedList<DateTime, LogEntry> _logsHistory = new SortedList<DateTime, LogEntry>();
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static ILogger _instance;
        private int logCount = 0;
        public bool hold = true;

        private Logger()
        { }

        public static ILogger Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger();
                    }
                    return _instance;
                }
            }
        }
        public IEnumerable<LogEntry> GetLogHistory()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<LogEntry>(_logsHistory.Values);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        public void GetReady()
        {
            logDirectory = ServerConfiguration.Instance.LogFolder;
            logFileName = ServerConfiguration.Instance.LogFile;
            maxLogFiles = ServerConfiguration.Instance.MaxLogFile;
            maxSizeFile = ServerConfiguration.Instance.SizeLogFile;
            //logLevel = ServerConfiguration.Instance.LogLevel;
        }

        public void LogDebug(string message, string moduleName = "default")
        {
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "DEBUG");
            preLog(logEntry);
        }

        public void LogInfo(string message, string moduleName = "default")
        {
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "INFO");
            preLog(logEntry);
        }

        public void LogWarning(string message, string moduleName = "default")
        {
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "WARNING");
            preLog(logEntry);
        }

        public void LogError(string message, string moduleName = "default")
        {
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "ERROR");
            preLog(logEntry);
        }

        public void preLog(LogEntry logEntry)
        {
            logEntry.ThreadID = Thread.CurrentThread.ManagedThreadId;


            logs.Enqueue(logEntry);
            historyLog(logEntry);

            if (logs.Count >= ServerConfiguration.Instance.MaxBufferLog && Server.Instance.Status.Equals("PandApache3 is up and running!"))
                flushLog();
        }

        private void historyLog(LogEntry logEntry)
        {
            if (ServerConfiguration.Instance.MaxHistoryLog <= 0)
                return;

            _lock.EnterWriteLock();
            try
            {

                if (logCount >= ServerConfiguration.Instance.MaxHistoryLog)
                    logCount = 0;

                if (_logsHistory.Count < ServerConfiguration.Instance.MaxHistoryLog)
                {
                    logCount++;
                }
                else
                {
                    _logsHistory.RemoveAt(0);
                }

                DateTime timestamp = logEntry.Timestamp;
                while (_logsHistory.ContainsKey(timestamp))
                {
                    timestamp = timestamp.AddTicks(1);
                }
                _logsHistory.Add(timestamp, logEntry);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error logging message: {ex.Message}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void flushLog()
        {
            LogEntry logEntry;

            Dictionary<string, StringBuilder> logsByModule = new Dictionary<string, StringBuilder>();
            logsByModule["default"] = new StringBuilder();
            logsByModule["Server"] = new StringBuilder();

            foreach (var moduleName in Server.Instance.Modules.Keys)
            {
                logsByModule[moduleName.ToString()] = new StringBuilder();
            }
            StringBuilder sbDefault = new StringBuilder();

            while (logs.Count > 0)
            {
                if (logs.TryDequeue(out logEntry))
                {
                    string log = ($"{logEntry.Timestamp,-20} - Module: {logEntry.Module,-10} - Thread ID: {logEntry.ThreadID,-2} - {logEntry.Level,-10} - {logEntry.Message}\n");
                    logsByModule[logEntry.Module].Append(log);
                }
            }

            foreach (var moduleName in logsByModule.Keys)
            {

                if (logsByModule[moduleName].Length > 0)
                {
                    Log(logsByModule[moduleName].ToString(), moduleName);
                }

            }
        }
        private void Log(string message, string module)
        {
            try
            {
                if (ServerConfiguration.Instance.LogToFile == true)
                {
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    foreach (var moduleConfig in ServerConfiguration.Instance.Modules)
                    {
                        if (moduleConfig.Name.Equals(module))
                        {
                            string logFilePath = Path.Combine(logDirectory, moduleConfig.Logger.LogFile);


                            if (File.Exists(logFilePath))
                            {
                                FileInfo fileInfo = new FileInfo(logFilePath);
                                if (fileInfo.Length > maxSizeFile)
                                {
                                    RotateLog(logFilePath);
                                }
                            }

                            using (StreamWriter sw = File.AppendText(logFilePath))
                            {
                                sw.Write($"{message}");
                            }

                            break;
                        }


                    }

                }

                if (ServerConfiguration.Instance.LogToConsole == true)
                {
                    Console.Write($"{message}");

                }

            }
            catch (Exception ex)
            {
                // En cas d'erreur lors de l'écriture du fichier log, affiche l'exception
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        private void RotateLog(string logFilePath)
        {
            try
            {

                // Renomme le fichier de log en ajoutant la date et l'heure actuelles au nom
                string newLogFilePath = Path.Combine(logDirectory, $"{Path.GetFileNameWithoutExtension(logFileName)}_{DateTime.Now:yyyy-MM-dd-HH-mm}{Path.GetExtension(logFileName)}");
                File.Move(logFilePath, newLogFilePath);

                // Supprime les anciens fichiers de log s'il y en a plus que le nombre maximum autorisé
                DeleteOldLogFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rotating log file: {ex.Message}");
            }
        }

        private void DeleteOldLogFiles()
        {
            try
            {
                // Récupère tous les fichiers de log dans le répertoire de logs
                DirectoryInfo directoryInfo = new DirectoryInfo(logDirectory);
                FileInfo[] logFiles = directoryInfo.GetFiles($"{Path.GetFileNameWithoutExtension(logFileName)}_*{Path.GetExtension(logFileName)}")
                                                   .OrderByDescending(f => f.CreationTime)
                                                   .ToArray();

                // Supprime les fichiers de log excédentaires
                for (int i = maxLogFiles; i < logFiles.Length; i++)
                {
                    logFiles[i].Delete();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error deleting old log files: {ex.Message}");
            }
        }
    }
}
