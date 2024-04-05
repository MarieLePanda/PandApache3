
namespace pandapache.src.LoggingAndMonitoring
{
    public interface ILogger
    {
        void Initialize(string logDirectory, string logFileName, int maxLogFiles, int maxSizeFile);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);

    }
}
