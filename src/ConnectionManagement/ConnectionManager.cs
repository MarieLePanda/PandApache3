using pandapache.src.Configuration;
using pandapache.src.ErrorHandling;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace pandapache.src.ConnectionManagement
{
    public class ConnectionManager
    {
        public TcpListener Listener { get; set; }
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public ConcurrentDictionary<Guid, ISocketWrapper> _clients { get; } = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private readonly ConcurrentDictionary<Guid, ISocketWrapper> _clientsRejected = new ConcurrentDictionary<Guid, ISocketWrapper>();
        private Func<HttpContext, Task> _pipeline;
        public async Task StartAsync(Func<HttpContext, Task> pipeline)
        {
            try
            {
                Listener = new TcpListener(ServerConfiguration.Instance.ServerIP, ServerConfiguration.Instance.ServerPort);
                Listener.Start();
                _pipeline = pipeline;
                Logger.LogInfo($"Server listening on {ServerConfiguration.Instance.ServerIP}:{ServerConfiguration.Instance.ServerPort}");
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

                Logger.LogInfo("Server stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping the server: {ex}");
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
                Request request = await ConnectionUtils.ParseRequestAsync(client);
                if (request == null)
                {
                    return;
                }
                HttpContext context = new HttpContext(request, null);
               
                await _pipeline(context);
                
                await ConnectionUtils.SendResponseAsync(client, context.Response);
                _clients.TryRemove(clientId, out client);
                client.Dispose();

                Logger.LogInfo($"Client closed");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error handling client: {ex.Message}");

            }
        }

        private async Task HandleClientRejectAsync(ISocketWrapper client, Guid clientId)
        {
            try
            {
                HttpResponse errorResponse = await ErrorHandler.HandleErrorAsync(new TooManyConnectionsException());
                await ConnectionUtils.SendResponseAsync(client, errorResponse);
 
                _clientsRejected.TryRemove(clientId, out client);
                client.Dispose();

                Logger.LogInfo("Closed");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error handling client: {ex.Message}");
            }
        }
    }
}
