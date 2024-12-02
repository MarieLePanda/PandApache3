using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Core.LoggingAndMonitoring
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public string Module { get; set; }
        public int ThreadID { get; set; }
        public LogEntry(DateTime timestamp, string message, string module, string level)
        {
            Timestamp = timestamp;
            Message = message;
            Module = module;
            Level = level;
        }
        public override string ToString()
        {
            return $"{Timestamp,-20} - Module: {Module,-10} - Thread ID: {ThreadID,-2} - {Level,-10} - {Message}";
        }
    }

}
