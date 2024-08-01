using pandapache.src.Configuration;
using pandapache.src.ErrorHandling;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace pandapache.src.ConnectionManagement
{
    public class ConnectionManager
    {
        public TcpListener Listener { get; set; }
        public bool IsAdmin { get; set; }
        public CancellationTokenSource _cancellationTokenSource { get; } = new CancellationTokenSource();
        public ConcurrentDictionary<Guid, ISocketWrapper> _clients { get; } = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private readonly ConcurrentDictionary<Guid, ISocketWrapper> _clientsRejected = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private Func<HttpContext, Task> _pipeline;

        public ConnectionManager(bool isAdmin) 
        {
            IsAdmin = isAdmin;
        }
        public async Task StartAsync(Func<HttpContext, Task> pipeline)
        {
            try
            {
                //To refactor in the futur to avoid the code duplication
                if (IsAdmin)
                {
                    Listener = new TcpListener(ServerConfiguration.Instance.ServerIP, ServerConfiguration.Instance.AdminPort);
                    Logger.LogInfo($"Admin server listening on {ServerConfiguration.Instance.ServerIP}:{ServerConfiguration.Instance.AdminPort}");

                }
                else
                {
                    Listener = new TcpListener(ServerConfiguration.Instance.ServerIP, ServerConfiguration.Instance.ServerPort);
                    Logger.LogInfo($"Web server listening on {ServerConfiguration.Instance.ServerIP}:{ServerConfiguration.Instance.ServerPort}");
                }

                Listener.Start();
                _pipeline = pipeline;


            }
            catch (SocketException ex)
            {
                Logger.LogError($"Error, port not available: {ex.Message}");

            }
            catch ( Exception ex )
            {
                Logger.LogError($"Error starting the server: {ex.Message}");

            }
        }

        public void Stop()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }

                if (Listener != null)
                {
                    Listener.Stop();
                }
                Logger.LogInfo("TCP listener stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping the TCP listener: {ex}");
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

                    Logger.LogInfo($"Client connected");
                    
                    // Handle client in a separate thread
                    Task.Run(() => HandleClientAsync(client, clientId));

                }
                else if(_clientsRejected.Count < ServerConfiguration.Instance.MaxRejectedConnections)
                {
                    Guid clientId = Guid.NewGuid();
                    _clientsRejected.TryAdd(clientId, client);

                    Logger.LogWarning("Too many connections - rejecting with HTTP 500");

                    Task.Run(() => HandleClientRejectAsync(client, clientId));

                }
                else
                {
                    Logger.LogError("Too many connection");
                    client.Dispose();

                    return;                
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error accepting client connection: {ex.Message}");

            }
        }

        private async Task HandleClientAsync(ISocketWrapper client, Guid clientId)
        {
            try
            {
                Thread currentThread = Thread.CurrentThread;
                Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) HandleClientAsync function");

                Request request = await ConnectionUtils.ParseRequestAsync(client);
                if (request == null)
                {
                    return;
                }
                HttpContext context = new HttpContext(request, null);
               
                await _pipeline(context);
                
                await ConnectionUtils.SendResponseAsync(client, context.Response);
                _clients.TryRemove(clientId, out client);
            }
            catch (Exception ex)
            {
                HttpResponse errorResponse = new HttpResponse(500)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(ex.Message))
                };
                await ConnectionUtils.SendResponseAsync(client, errorResponse);
                Logger.LogError($"Error handling client: {ex.Message}");

            }
            finally
            {
                client.Dispose();
                Logger.LogInfo(" client Closed");

            }

        }

        private async Task HandleClientRejectAsync(ISocketWrapper client, Guid clientId)
        {
            try
            {
                HttpResponse errorResponse = await ErrorHandler.HandleErrorAsync(new TooManyConnectionsException());
                await ConnectionUtils.SendResponseAsync(client, errorResponse);
 
                _clientsRejected.TryRemove(clientId, out client);

            }
            catch (Exception ex)
            {
                HttpResponse errorResponse = new HttpResponse(500)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(ex.Message))
                };
                await ConnectionUtils.SendResponseAsync(client, errorResponse);

                Logger.LogError($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Dispose();
                Logger.LogInfo(" client Closed");

            }
        }
    }
}
