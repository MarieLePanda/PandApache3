using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using PandApache3.src.Module;
using System.Diagnostics;

class Startup
{
    public static int PROCESSID { get ; set; } 
    public static string PROCESSNAME { get; set; }
    private static Func<HttpContext, Task> webPipeline;
    private static Func<HttpContext, Task> adminPipeline;

    private static ConnectionManagerModule _ConnectionManagerWeb = null;
    private static ConnectionManagerModule _ConnectionManagerAdmin = null;
    private static CancellationTokenSource _cancellationTokenSourceServer = new CancellationTokenSource();
    
    private static int _retry = 1;
    private static readonly object _lock = new object();


    static async Task Main(string[] args)
    {
        Logger logger = (Logger)Logger.Instance;
        Server PandApache3 = Server.Instance;

        while (PandApache3.CancellationTokenSource.IsCancellationRequested == false)
        {
             Thread.Sleep(1000);
            if (PandApache3.Status.Equals("PandApache3 is stopping"))
                continue;

            if(PandApache3.CancellationTokenSource.IsCancellationRequested == true)
            {
                continue;
            }

            string banner = @"
            ██████╗  █████╗ ███╗   ██╗██████╗  █████╗ 
            ██╔══██╗██╔══██╗████╗  ██║██╔══██╗██╔══██╗
            ██████╔╝███████║██╔██╗ ██║██║  ██║███████║
            ██╔═══╝ ██╔══██║██║╚██╗██║██║  ██║██╔══██║
            ██║     ██║  ██║██║ ╚████║██████╔╝██║  ██║
            ╚═╝     ╚═╝  ╚═╝╚═╝  ╚═══╝╚═════╝ ╚═╝  ╚═╝
                                          
            ██████╗  █████╗  ██████╗██╗  ██╗███████╗  
            ██╔══██╗██╔══██╗██╔════╝██║  ██║██╔════╝  
            ██████╔╝███████║██║     ███████║█████╗    
            ██╔═══╝ ██╔══██║██║     ██╔══██║██╔══╝    
            ██║     ██║  ██║╚██████╗██║  ██║███████╗  
            ╚═╝     ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝  
                                          
            ██████╗    ██████╗                        
            ╚════██╗   ╚════██╗                       
             █████╔╝    █████╔╝                       
             ╚═══██╗    ╚═══██╗                       
            ██████╔╝██╗██████╔╝                       
            ╚═════╝ ╚═╝╚═════╝                        
            ";

            PandApache3.Init();
            PandApache3.StartAsync();
            
            logger.LogInfo(banner);
            logger.GetReady();
            logger.flushLog();
            
            PROCESSID = Process.GetCurrentProcess().Id;
            PROCESSNAME = Process.GetCurrentProcess().ProcessName;
            Logger.Instance.LogInfo($"PandApache3 process id:{PROCESSID}");
            Logger.Instance.LogInfo($"PandApache3 process name:{PROCESSNAME}");
            
            await PandApache3.RunAsync();

        }

        logger.LogInfo("La revedere !");
        logger.flushLog();
    }



}