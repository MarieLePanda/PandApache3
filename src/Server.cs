using pandapache.src.Configuration;
using pandapache.src.ConnectionManagement;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using pandapache.src.ResponseGeneration;
using PandApache3.src.Middleware;
using PandApache3.src.ResponseGeneration;

class Program
{
    static async Task Main(string[] args)
    {
        Logger.Initialize();
        ServerConfiguration.Instance.ReloadConfiguration();
        ServerConfiguration.Instance.Export("exportConfig.conf");
        ConnectionManager connectionManager = new ConnectionManager();

        IFileManager fileManager = FileManagerFactory.Instance();

        TerminalMiddleware terminalMiddleware = new TerminalMiddleware();
        RoutingMiddleware routingMiddleware = new RoutingMiddleware(terminalMiddleware.InvokeAsync, fileManager);

        DirectoryMiddleware directoryMiddleware = new DirectoryMiddleware(routingMiddleware.InvokeAsync);

        AuthenticationMiddleware authenticationMiddleware = new AuthenticationMiddleware(directoryMiddleware.InvokeAsync);
        LoggerMiddleware loggerMiddleware = new LoggerMiddleware(authenticationMiddleware.InvokeAsync);
        Func<HttpContext, Task> pipeline = loggerMiddleware.InvokeAsync;

        connectionManager.StartAsync(pipeline);


        while (true)
        {
            if (connectionManager.Listener.Pending())
            {
                ISocketWrapper client = new SocketWrapper(connectionManager.Listener.AcceptSocket());
                await connectionManager.AcceptConnectionsAsync(client);
            }

        }
    }
}