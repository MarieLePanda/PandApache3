using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.LoggingAndMonitoring
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

        public LogEntry(DateTime timestamp, string message)
        {
            Timestamp = timestamp;
            Message = message;
        }
    }

}
