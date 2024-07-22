using pandapache.src.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace pandapache.src.LoggingAndMonitoring
{
    public static class Logger
    {

        private static string logDirectory;
        private static string logFileName;
        private static int maxLogFiles;
        private static int maxSizeFile;
        private static string logLevel;
        private static int maxBufferSize = 100;
        private static ConcurrentQueue<string> logs = new ConcurrentQueue<string>();
        public static bool hold = true;


        public static void Initialize()
        {
            Logger.logDirectory = ServerConfiguration.Instance.LogFolder;
            Logger.logFileName = ServerConfiguration.Instance.LogFile;
            Logger.maxLogFiles = ServerConfiguration.Instance.MaxLogFile;
            Logger.maxSizeFile = ServerConfiguration.Instance.SizeLogFile;
            Logger.logLevel = ServerConfiguration.Instance.LogLevel;
        }

        public static void LogDebug(string message)
        {
            if (new List<string> {"debug"}.Contains(ServerConfiguration.Instance.LogLevel))
            {
                Thread currentThread = Thread.CurrentThread;
                preLog($"{DateTime.Now} - Thread ID: {currentThread.ManagedThreadId} - [DEBUG] - {message}");
            }
        }

        public static void LogInfo(string message)
        {
            if (new List<string> { "debug", "info" }.Contains(ServerConfiguration.Instance.LogLevel))
            {
                Thread currentThread = Thread.CurrentThread;
                preLog($"{DateTime.Now} - Thread ID: {currentThread.ManagedThreadId} - [INFO] - {message}");
            }
        }

        public static void LogWarning(string message)
        {
            if (new List<string> { "debug", "info", "warning" }.Contains(ServerConfiguration.Instance.LogLevel))
            {
                Thread currentThread = Thread.CurrentThread;
                preLog($"{DateTime.Now} - Thread ID: {currentThread.ManagedThreadId} - [WARNING] - {message}");
            }
        }

        public static void LogError(string message)
        {
            if (new List<string> { "debug", "info", "warning", "error"  }.Contains(ServerConfiguration.Instance.LogLevel))
            {
                Thread currentThread = Thread.CurrentThread;
                preLog($"{DateTime.Now} - Thread ID: {currentThread.ManagedThreadId} - [ERROR] - {message}");
            }
        }

        private static void preLog(string message)
        {
           logs.Enqueue(message);

            if (logs.Count >= ServerConfiguration.Instance.MaxBufferLog)
                flushLog();
        }

        public static void flushLog()
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
        private static void Log(string message)
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

        private static void RotateLog()
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

        private static void DeleteOldLogFiles()
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
