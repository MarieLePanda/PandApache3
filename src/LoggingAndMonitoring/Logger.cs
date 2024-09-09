using pandapache.src.Configuration;
using PandApache3.src.LoggingAndMonitoring;
using PandApache3.src.Module;
using System.Collections.Concurrent;
using System.Text;
using ExecutionContext = PandApache3.src.Module.ExecutionContext;

namespace pandapache.src.LoggingAndMonitoring
{
    public class Logger
    {

        private string logDirectory;
        private string logFileName;
        private int maxLogFiles;
        private int maxSizeFile;
        //private string logLevel;
        private int maxBufferSize = 100;
        private ConcurrentQueue<string> logs = new ConcurrentQueue<string>();
        private SortedList<DateTime, LogEntry> _logsHistory = new SortedList<DateTime, LogEntry>();
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static Logger _instance;
        private int logCount = 0;        
        public bool hold = true;

        private Logger()
        {

        }

        public static Logger Instance
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

        public void LogDebug(string message, string module="default")
        {
//            if (new List<string> {"debug"}.Contains(ExecutionContext.Current.Logger.LogLevel))
                preLog("DEBUG",message, module);
        }

        public void LogInfo(string message, string module="default")
        {
//            if (new List<string> { "debug", "info" }.Contains(ExecutionContext.Current.Logger.LogLevel))
                preLog("INFO", message, module);
        }

        public void LogWarning(string message, string module="default")
        {
//            if (new List<string> { "debug", "info", "warning" }.Contains(ExecutionContext.Current.Logger.LogLevel))
                preLog("WARNING", message, module);

        }

        public void LogError(string message, string module="default")
        {
//            if (new List<string> { "debug", "info", "warning", "error"  }.Contains(ExecutionContext.Current.Logger.LogLevel))
                preLog("ERROR", message, module);

        }

        public void preLog(string level, string message, string module)
        {
            DateTime timestamp = DateTime.Now;
            Thread currentThread = Thread.CurrentThread;
            string log = $"{timestamp} - MODULE: {module} - Thread ID: {currentThread.ManagedThreadId} - [{level}] - {message}";

            logs.Enqueue(log);
            LogEntry logEntry = new LogEntry(timestamp, log);
            
            historyLog(logEntry);

            if (logs.Count >= ServerConfiguration.Instance.MaxBufferLog &&  Server.Instance.Status.Equals("PandApache3 is up and running!"))
                flushLog();
        }

        private void historyLog(LogEntry logEntry)
        {
            if(ServerConfiguration.Instance.MaxHistoryLog <= 0)
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
            string message = string.Empty;
            StringBuilder sb = new StringBuilder();
            while (logs.Count > 0)
            {
                if (logs.TryDequeue(out message))
                {
                    sb.AppendLine(message);
                }
            }

            if (sb.Length > 0)
            {
                Log(sb.ToString());
            }
        }
        private void Log(string message)
        {
            try
            {
                if (ServerConfiguration.Instance.LogToFile == true)
                {
                    // Vérifie si le répertoire de logs existe, sinon le crée
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    // Crée le chemin complet pour le fichier log
                    string logFilePath = Path.Combine(logDirectory, logFileName);

                    // Vérifie si le fichier de log dépasse la taille maximale
                    if (File.Exists(logFilePath))
                    {
                        FileInfo fileInfo = new FileInfo(logFilePath);
                        if (fileInfo.Length > maxSizeFile)
                        {
                            RotateLog();
                        }
                    }

                    // Écrit le message dans le fichier log, en ajoutant la date et l'heure actuelles
                    using (StreamWriter sw = File.AppendText(logFilePath))
                    {
                        sw.Write($"{message}");
                    }

                }

                if(ServerConfiguration.Instance.LogToConsole == true)
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

        private void RotateLog()
        {
            try
            {
                // Crée le chemin complet pour le fichier log
                string logFilePath = Path.Combine(logDirectory, logFileName);

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
