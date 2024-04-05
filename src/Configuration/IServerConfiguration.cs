using System.Net;

namespace pandapache.src.Configuration
{
    public interface IServerConfiguration
    {
         //General configuration
        public string ServerName { get; }
        public IPAddress ServerIP { get; set; }
        public int ServerPort { get; set; }

        //Performance
        public int MaxAllowedConnections { get; set; }
        public int MaxRejectedConnections { get; set; }

        //Logging
        public string LogFolder { get; set; }
        public string LogFile { get; set; }
        public int MaxLogFile { get; set; }
        public int SizeLogFile { get; set; }
        public string LogLevel { get; set; }
        //Routing
        public string RootDirectory { get; set; }
        public string Persistence { get; set; }
        //Other
       
    }
}
