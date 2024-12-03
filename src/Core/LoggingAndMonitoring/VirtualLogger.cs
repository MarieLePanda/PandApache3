
using PandApache3.src.Core.Configuration;

namespace PandApache3.src.Core.LoggingAndMonitoring
{
    public class VirtualLogger : ILogger
    {
        //private Logger _singletonLogger;
        private string _moduleName;
        public string LogLevel;
        public string LogFile;

        public VirtualLogger(string moduleName)
        {
            _moduleName = moduleName;
            LogLevel = ServerConfiguration.Instance.LogLevel;
            LogFile = ServerConfiguration.Instance.LogFile;
        }

        public void LogDebug(string message, string moduleName = "default")
        {
            moduleName = _moduleName;
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "DEBUG");
            if (new List<string> { "debug" }.Contains(LogLevel))
                Logger.Instance.preLog(logEntry);
        }

        public void LogInfo(string message, string moduleName = "default")
        {
            moduleName = _moduleName;
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "INFO");
            if (new List<string> { "debug", "info" }.Contains(LogLevel))
                Logger.Instance.preLog(logEntry);
        }

        public void LogWarning(string message, string moduleName = "default")
        {
            moduleName = _moduleName;
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "WARNING");
            if (new List<string> { "debug", "info", "warning" }.Contains(LogLevel))
                Logger.Instance.preLog(logEntry);

        }

        public void LogError(string message, string moduleName = "default")
        {
            moduleName = _moduleName;
            LogEntry logEntry = new LogEntry(DateTime.Now, message, moduleName, "ERROR");
            if (new List<string> { "debug", "info", "warning", "error" }.Contains(LogLevel))
                Logger.Instance.preLog(logEntry);

        }

        public void preLog(LogEntry logEntry)
        {
            throw new NotImplementedException();
        }
    }
}
