using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.LoggingAndMonitoring
{
    public class VirtualLogger
    {
        //private Logger _singletonLogger;
        private string _moduleName;
        public string LogLevel;

        public VirtualLogger(string moduleName, string logLevel="info")
        {
            _moduleName = moduleName;
            LogLevel = logLevel;
        }

        public void LogDebug(string message)
        {
            if (new List<string> { "debug" }.Contains(LogLevel))
                Logger.Instance.preLog("DEBUG", message, _moduleName);
        }

        public void LogInfo(string message)
        {
            if (new List<string> { "debug", "info" }.Contains(LogLevel))
                Logger.Instance.preLog("INFO", message, _moduleName);
        }

        public void LogWarning(string message)
        {
            if (new List<string> { "debug", "info", "warning" }.Contains(LogLevel))
                Logger.Instance.preLog("WARNING", message, _moduleName);

        }

        public void LogError(string message)
        {
            if (new List<string> { "debug", "info", "warning", "error" }.Contains(LogLevel))
                Logger.Instance.preLog("ERROR", message, _moduleName);

        }
    }
}
