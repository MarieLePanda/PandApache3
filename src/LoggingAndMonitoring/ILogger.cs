﻿
using PandApache3.src.LoggingAndMonitoring;

namespace pandapache.src.LoggingAndMonitoring
{
    public interface ILogger
    {
        void LogDebug(string message, string module="default");
        void LogInfo(string message, string module = "default");
        void LogWarning(string message, string module = "default");
        void LogError(string message, string module = "default");
        public void preLog(LogEntry logEntry);


    }
}
