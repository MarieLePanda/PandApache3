using pandapache.src.Configuration;
using pandapache.src.ConnectionManagement;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using pandapache.src.ResponseGeneration;
using PandApache3.src.Middleware;
using PandApache3.src.ResponseGeneration;

class Server
{
    public static string STATUS {  get; set; }
    private static ConnectionManager _ConnectionManager = null;
    static async Task Main(string[] args)
    {
        Server.STATUS = "PandApache3 is starting";

        Logger.Initialize();
        ServerConfiguration.Instance.ReloadConfiguration();
        //ServerConfiguration.Instance.Export("exportConfig.conf");
        _ConnectionManager = new ConnectionManager();

        IFileManager fileManager = FileManagerFactory.Instance();

        TerminalMiddleware terminalMiddleware = new TerminalMiddleware();
        RoutingMiddleware routingMiddleware = new RoutingMiddleware(terminalMiddleware.InvokeAsync, fileManager);

        DirectoryMiddleware directoryMiddleware = new DirectoryMiddleware(routingMiddleware.InvokeAsync);

        AuthenticationMiddleware authenticationMiddleware = new AuthenticationMiddleware(directoryMiddleware.InvokeAsync);
        LoggerMiddleware loggerMiddleware = new LoggerMiddleware(authenticationMiddleware.InvokeAsync);
        Func<HttpContext, Task> pipeline = loggerMiddleware.InvokeAsync;

        await _ConnectionManager.StartAsync(pipeline);

        Server.STATUS = "PandApache3 is up and running !";

        while (true)
        {
            try
            {
                if (_ConnectionManager.Listener.Pending())
                {
                    ISocketWrapper client = new SocketWrapper(_ConnectionManager.Listener.AcceptSocket());
                    await _ConnectionManager.AcceptConnectionsAsync(client);
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug($"Error with Listener pending: {e.Message}");
            }
            

        }
    }

    public static async Task StopServer()
    {
        int retry = 5;
        Server.STATUS = "PandApache3 is stopping";
        Logger.LogInfo(STATUS);
        _ConnectionManager.Listener.Stop();

        for (int i = 0; i < retry; retry--)
        {
            if (_ConnectionManager._clients.Count > 0)
            {
                Thread.Sleep(1000);
                Logger.LogDebug("There is still active connection...");
            }
        }

        if (retry > 0)
        {

            Logger.LogInfo("Force server to stop ");
        }
        {
            Logger.LogInfo("Stopping server");
        }
        
        System.Environment.Exit(0);
    }
}