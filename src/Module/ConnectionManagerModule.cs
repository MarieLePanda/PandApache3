using pandapache.src;
using pandapache.src.Configuration;
using pandapache.src.ConnectionManagement;
using pandapache.src.ErrorHandling;
using pandapache.src.RequestHandling;
using PandApache3.src.LoggingAndMonitoring;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace PandApache3.src.Module
{
    public class ConnectionManagerModule : IModule
    {
        public TcpListener Listener { get; set; }
        public TaskFactory TaskFactory { get; }
        public ModuleInfo ModuleInfo { get; set; }
        public ModuleType ModuleType { get; set; }
        private static AsyncLocal<ModuleInfo> _current = new AsyncLocal<ModuleInfo>();
        public CancellationTokenSource _cancellationTokenSource { get; } = new CancellationTokenSource();
        private ConcurrentDictionary<Guid, ISocketWrapper> _clients { get; } = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private ConcurrentDictionary<Guid, ISocketWrapper> _clientsRejected = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private Func<HttpContext, Task> _pipeline;
        private TaskScheduler _taskScheduler; 


        private int _retry = 3;
        public ConnectionManagerModule(ModuleType moduleType, Func<HttpContext, Task> pipeline, TaskScheduler taskScheduler)
        {
            ModuleType = moduleType;
            _pipeline = pipeline;
            _taskScheduler = taskScheduler;
            TaskFactory = new TaskFactory(_taskScheduler);

            bool moduleInfoExist = false;
            foreach (ModuleInfo moduleInfo in ServerConfiguration.Instance.Modules)
            {
                if (moduleInfo.Type == ModuleType)
                {
                    moduleInfoExist = true;
                    ModuleInfo = moduleInfo;
                }
            }

            if (!moduleInfoExist)
            {
                ModuleInfo = new ModuleInfo(ModuleType.ToString())
                {
                    isEnable = true,
                };
            }

            ModuleInfo.Logger = new VirtualLogger(moduleType.ToString(), "info");



        }

        public async Task StartAsync()
        {
            ExecutionContext.Current = ModuleInfo;
            ExecutionContext.Current.Logger.LogInfo("Starting Connection manager module");

            Server.Instance.CancellationTokens.Add(ModuleInfo.Name, _cancellationTokenSource);

            try
            {
                //To refactor in the futur to avoid the code duplication
                if (ModuleType.Equals(ModuleType.Admin))
                {
                    Listener = new TcpListener(ServerConfiguration.Instance.ServerIP, ServerConfiguration.Instance.AdminPort);
                    ExecutionContext.Current.Logger.LogInfo($"Admin server listening on {ServerConfiguration.Instance.ServerIP}:{ServerConfiguration.Instance.AdminPort}");

                }
                else
                {
                    Listener = new TcpListener(ServerConfiguration.Instance.ServerIP, ServerConfiguration.Instance.ServerPort);
                    ExecutionContext.Current.Logger.LogInfo($"Web server listening on {ServerConfiguration.Instance.ServerIP}:{ServerConfiguration.Instance.ServerPort}");
                }

                Listener.Start();
            }
            catch (SocketException ex)
            {
                ExecutionContext.Current.Logger.LogError($"Error, port not available: {ex.Message}");

            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"Error starting the server: {ex.Message}");

            }
        }

        public async Task RunAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            ExecutionContext.Current.Logger.LogInfo("Running Connection manager module");

            Thread currentThread = Thread.CurrentThread;
            ExecutionContext.Current.Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function RunServerAsync");

            do
            {
                try
                {
                    if (Listener.Pending())
                    {
                        ISocketWrapper client = new SocketWrapper(Listener.AcceptSocket());
                        await AcceptConnectionsAsync(client);
                    }
                }
                catch (Exception e)
                {
                    ExecutionContext.Current.Logger.LogError($"Error with Listener pending: {e.Message}");
                    Thread.Sleep(1000 * _retry);
                    _retry++;
                    ExecutionContext.Current.Logger.LogError($"Cancelation token status: {Server.Instance.CancellationTokens[ModuleInfo.Name].Token.IsCancellationRequested}");
                }


            } while (Server.Instance.CancellationTokens[ModuleInfo.Name].Token.IsCancellationRequested == false);

            ExecutionContext.Current.Logger.LogWarning($"Task {currentThread.ManagedThreadId} run server exited");
        }

        public async Task StopAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            ExecutionContext.Current.Logger.LogInfo("Stopping Connection manager module");

            try
            {
                int retry = 5;
                Listener.Stop();
                ExecutionContext.Current.Logger.LogInfo("TCP listener stopped.");

                for (int i = 0; i < retry; retry--)
                {
                    if (_clients.Count > 0)
                    {
                        Thread.Sleep(1000);
                        ExecutionContext.Current.Logger.LogDebug("There are still active connections...");
                    }
                    else
                    {
                        break;
                    }
                }
                if (retry == 0)
                {
                    ExecutionContext.Current.Logger.LogInfo("Force connection to close...");
                    foreach (var clientGuid in _clients.Keys)
                    {
                        _clients[clientGuid].Dispose();
                    }
                }
                ExecutionContext.Current.Logger.LogInfo("No more connection");
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"Error stopping the TCP listener: {ex.Message}");
            }


        }

    public async Task AcceptConnectionsAsync(ISocketWrapper client)
        {
            try
            {
                if (_clients.Count < ServerConfiguration.Instance.MaxAllowedConnections)
                {

                    Guid clientId = Guid.NewGuid();
                    _clients.TryAdd(clientId, client);

                    ExecutionContext.Current.Logger.LogInfo($"Client connected");

                    // Handle client in a separate thread
                    TaskFactory.StartNew(() => HandleClientAsync(client, clientId));


                }
                else if (_clientsRejected.Count < ServerConfiguration.Instance.MaxRejectedConnections)
                {
                    Guid clientId = Guid.NewGuid();
                    _clientsRejected.TryAdd(clientId, client);

                    ExecutionContext.Current.Logger.LogWarning("Too many connections - rejecting with HTTP 500");

                    TaskFactory.StartNew(() => HandleClientRejectAsync(client, clientId));

                }
                else
                {
                    ExecutionContext.Current.Logger.LogError("Too many connection");
                    client.Dispose();

                    return;
                }
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"Error accepting client connection: {ex.Message}");

            }
        }

        private async Task HandleClientAsync(ISocketWrapper client, Guid clientId)
        {
            try
            {
                Thread currentThread = Thread.CurrentThread;
                ExecutionContext.Current.Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) HandleClientAsync function");

                var taskParsing = TaskFactory.StartNew(() => ConnectionUtils.ParseRequestAsync(client)).Unwrap();
                Request request = await taskParsing;
                if (request == null)
                {
                    return;
                }
                HttpContext context = new HttpContext(request, null);


                var taskPipeline = TaskFactory.StartNew(() => _pipeline(context)).Unwrap();
                await taskPipeline;

                var taskSending = TaskFactory.StartNew(() => ConnectionUtils.SendResponseAsync(client, context.Response, context.Request)).Unwrap();

                await taskSending;

                _clients.TryRemove(clientId, out client);
            }
            catch (Exception ex)
            {
                HttpResponse errorResponse = new HttpResponse(500)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(ex.Message))
                };
                await ConnectionUtils.SendResponseAsync(client, errorResponse, null);
                ExecutionContext.Current.Logger.LogError($"Error handling client: {ex.Message}");

            }
            finally
            {
                client.Dispose();
                ExecutionContext.Current.Logger.LogInfo(" client Closed");

            }

        }

        private async Task HandleClientRejectAsync(ISocketWrapper client, Guid clientId)
        {
            try
            {
                HttpResponse errorResponse = await ErrorHandler.HandleErrorAsync(new TooManyConnectionsException());
                await ConnectionUtils.SendResponseAsync(client, errorResponse, null);

                _clientsRejected.TryRemove(clientId, out client);

            }
            catch (Exception ex)
            {
                HttpResponse errorResponse = new HttpResponse(500)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(ex.Message))
                };
                await ConnectionUtils.SendResponseAsync(client, errorResponse, null);

                ExecutionContext.Current.Logger.LogError($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Dispose();
                ExecutionContext.Current.Logger.LogInfo(" client Closed");

            }
        }

        public bool isEnable()
        {
            return ModuleInfo.isEnable;
        }

        VirtualLogger IModule.Logger()
        {
            return null;
        }
    }
}
