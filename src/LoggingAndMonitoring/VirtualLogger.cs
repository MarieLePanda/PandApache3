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
        private LogConfiguration _logConfiguration;
        private string _logLevel;

        public VirtualLogger(string moduleName, string logLevel)
        {
            _moduleName = moduleName;
            _logLevel = logLevel;
        }

        public void LogDebug(string message)
        {
            if (new List<string> { "debug" }.Contains(ServerConfiguration.Instance.LogLevel))
                Logger.Instance.preLog("DEBUG", message, _moduleName);
        }

        public void LogInfo(string message)
        {
            if (new List<string> { "debug", "info" }.Contains(ServerConfiguration.Instance.LogLevel))
                Logger.Instance.preLog("INFO", message, _moduleName);
        }

        public void LogWarning(string message)
        {
            if (new List<string> { "debug", "info", "warning" }.Contains(ServerConfiguration.Instance.LogLevel))
                Logger.Instance.preLog("WARNING", message, _moduleName);

        }

        public void LogError(string message)
        {
            if (new List<string> { "debug", "info", "warning", "error" }.Contains(ServerConfiguration.Instance.LogLevel))
                Logger.Instance.preLog("ERROR", message, _moduleName);

        }
    }
}
