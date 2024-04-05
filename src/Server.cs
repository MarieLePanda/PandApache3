using pandapache.src.ConnectionManagement;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using pandapache.src.ResponseGeneration;
using PandApache3.src.ResponseGeneration;

class Program
{
    static async Task Main(string[] args)
    {
        ConnectionManager connectionManager = new ConnectionManager();
        Logger.Initialize();
        IFileManager fileManager = FileManagerFactory.Instance();

        TerminalMiddleware terminalMiddleware = new TerminalMiddleware();
        RoutingMiddleware routingMiddleware = new RoutingMiddleware(terminalMiddleware.InvokeAsync, fileManager);
        LoggerMiddleware loggerMiddleware = new LoggerMiddleware(routingMiddleware.InvokeAsync);
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