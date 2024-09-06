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
        private Logger _singletonLogger;
        private string _moduleName;
        private LogConfiguration _logConfiguration;
        private string 
        public ModuleLogger(string moduleName, string logLevel)
        {
            _singletonLogger = SingletonLogger.Instance;
            _moduleName = moduleName;
            _logLevel = logLevel;
        }
    }
}
