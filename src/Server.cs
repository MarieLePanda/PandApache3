using pandapache.src;
using pandapache.src.Configuration;
using pandapache.src.ConnectionManagement;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using pandapache.src.ResponseGeneration;
using PandApache3.src.Middleware;
using PandApache3.src.ResponseGeneration;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

class Server
{
    public static string STATUS {  get; set; }
    private static ConnectionManager _ConnectionManagerWeb = null;
    private static ConnectionManager _ConnectionManagerAdmin = null;
    private static CancellationTokenSource _cancellationTokenSourceServer = new CancellationTokenSource();
    
    private static int _retry = 1;
    private static readonly object _lock = new object();


    static async Task Main(string[] args)
    {

        Server.STATUS = "PandApache3 is stopped";
        while (_cancellationTokenSourceServer.IsCancellationRequested == false)
        {
             Thread.Sleep(1000);
            if (Server.STATUS.Equals("PandApache3 is stopping"))
                continue;

            if(_cancellationTokenSourceServer.IsCancellationRequested == true)
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
            Logger.LogInfo(banner);

            await StartServerAsync();
            await RunAllServerAsync();

           

        }

        Logger.LogInfo("La revedere !");
    }

    private static async Task RunAllServerAsync()
    {

        Server.STATUS = "PandApache3 is up and running!";
        Logger.LogInfo($"{Server.STATUS}");

        Task webServerTask = Task.Run(() => RunServerAsync(_ConnectionManagerWeb));
        Task adminServerTask = Task.Run(() => RunServerAsync(_ConnectionManagerAdmin));

        // Wait for both tasks to complete
        await Task.WhenAll(webServerTask, adminServerTask);


    }
    private static async Task RunServerAsync(ConnectionManager connectionManager)
    {

        Thread currentThread = Thread.CurrentThread;
        Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function RunServerAsync");

        TcpListener listener = connectionManager.Listener;
        do
        {
            try
            {
                if (listener.Pending())
                {
                    ISocketWrapper client = new SocketWrapper(listener.AcceptSocket());
                    await connectionManager.AcceptConnectionsAsync(client);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error with Listener pending: {e.Message}");
                Thread.Sleep(1000 * _retry);
                _retry++;
                Logger.LogError($"Cancelation token status: {connectionManager._cancellationTokenSource.Token.IsCancellationRequested}");
            }


        } while (connectionManager._cancellationTokenSource.IsCancellationRequested == false);

        Logger.LogWarning($"Task {currentThread.ManagedThreadId} run server exited");
    }
    public static async Task StartServerAsync(List<CancellationTokenSource> cancellationTokens=null)
    {
        Thread currentThread = Thread.CurrentThread;
        Console.WriteLine($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function StartServerAsync");

        Server.STATUS = "PandApache3 is starting";
        Logger.LogInfo($"{Server.STATUS}");

        if (cancellationTokens != null)
        {

            foreach (CancellationTokenSource tokenSource in cancellationTokens)
            {
                Logger.LogInfo($"Cancel previous token {tokenSource.Token}");
                tokenSource.Cancel();
            }
        }
        Logger.Initialize();
        ServerConfiguration.Instance.ReloadConfiguration();

        _ConnectionManagerAdmin = new ConnectionManager(true);
        _ConnectionManagerWeb = new ConnectionManager(false);

        IFileManager fileManager = FileManagerFactory.Instance();

        TerminalMiddleware terminalMiddleware = new TerminalMiddleware();
        RoutingMiddleware routingMiddleware = new RoutingMiddleware(terminalMiddleware.InvokeAsync, fileManager);
        DirectoryMiddleware directoryMiddleware = new DirectoryMiddleware(routingMiddleware.InvokeAsync);
        AuthenticationMiddleware authenticationMiddleware = new AuthenticationMiddleware(directoryMiddleware.InvokeAsync);
        LoggerMiddleware loggerMiddleware = new LoggerMiddleware(authenticationMiddleware.InvokeAsync);
        Func<HttpContext, Task> webPipeline = loggerMiddleware.InvokeAsync;


        AdminMiddleware adminMiddleware = new AdminMiddleware(terminalMiddleware.InvokeAsync);
        DirectoryMiddleware adminDirectoryMiddleware = new DirectoryMiddleware(adminMiddleware.InvokeAsync);
        AuthenticationMiddleware adminAuthenticationMiddleware = new AuthenticationMiddleware(adminDirectoryMiddleware.InvokeAsync);
        LoggerMiddleware adminLoggerMiddleware = new LoggerMiddleware(adminAuthenticationMiddleware.InvokeAsync);
        Func<HttpContext, Task> adminPipeline = adminLoggerMiddleware.InvokeAsync;


        await _ConnectionManagerWeb.StartAsync(webPipeline);
        await _ConnectionManagerAdmin.StartAsync(adminPipeline);

        Server.STATUS = "PandApache3 is started";



    }

    public static async Task StoppingServerAsync(bool isRestart=false)
    {


        Thread currentThread = Thread.CurrentThread;
        Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function RestartServerAsync");

        if (!Monitor.TryEnter(_lock))
        {
            Logger.LogDebug($"Thread (Thread ID: {Thread.CurrentThread.ManagedThreadId}) could not acquire the lock and will exit.");
            Logger.LogInfo("Server is already restarting");
            return;
        }

        lock (_lock)
        {
            List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();
            cancellationTokens.Add(_ConnectionManagerAdmin._cancellationTokenSource);
            cancellationTokens.Add(_ConnectionManagerWeb._cancellationTokenSource);

            StopServerAsync(isRestart, cancellationTokens).Wait();
        }

        Logger.LogDebug("Get out of the lock");
        Monitor.Exit(_lock);
    }


    public static async Task StopServerAsync(bool isRestart, List<CancellationTokenSource> cancellationTokens=null)
    {

        Thread currentThread = Thread.CurrentThread;
        Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function StopServerAsync");


        int retry = 5;
        Server.STATUS = "PandApache3 is stopping";
        Logger.LogInfo($"{Server.STATUS}");

        if (cancellationTokens != null)
        {
            foreach (CancellationTokenSource token in cancellationTokens)
            {
                Logger.LogInfo($"Token to cancel: {token} - status: {token.IsCancellationRequested}");
                token.Cancel();
                Logger.LogInfo($"Token to cancel: {token} - status: {token.IsCancellationRequested}");
            }
        }

        _ConnectionManagerAdmin.Listener.Stop();
        _ConnectionManagerWeb.Listener.Stop();

        for (int i = 0; i < retry; retry--)
        {
            if (_ConnectionManagerAdmin._clients.Count > 0 || _ConnectionManagerWeb._clients.Count > 0)
            {
                Thread.Sleep(1000);
                Logger.LogDebug("There is still active connection...");
            }
            else
            {
                break;
            }
        }

        if (retry == 0)
        {

            Logger.LogInfo("Force server to stop ");
        }
        else
        {
            Logger.LogInfo("Server stopped");
        }

        // Get all threads in the current process
        ProcessThreadCollection threads = Process.GetCurrentProcess().Threads;

        Logger.LogDebug($"Current Threads ({threads.Count}):");
        foreach (ProcessThread thread in threads)
        {
            Logger.LogDebug($"Thread ID: {thread.Id}, State: {thread.ThreadState}");
        }

        Server.STATUS = "PandApache3 is stopped";
        Logger.LogInfo($"{Server.STATUS}");

        if (isRestart == false)
        {
            Logger.LogInfo($"global Server token status {_cancellationTokenSourceServer.Token.IsCancellationRequested}");
            _cancellationTokenSourceServer.Cancel();
            Logger.LogInfo($"global Server token status {_cancellationTokenSourceServer.Token.IsCancellationRequested}");
            
        }
    }


}