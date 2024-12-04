using pandapache.src;
using PandApache3.src.Core.RequestHandling;
using System.Net.Sockets;
using System.Text;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Core.ConnectionManagement
{
    public static class ConnectionUtils
    {


        public async static Task<Request?> ParseRequestAsync(ISocketWrapper client)
        {

            
            byte[] bufferRequest = new byte[1024];
            int bytesRead = client.Receive(bufferRequest);
            string requestString = Encoding.UTF8.GetString(bufferRequest, 0, bytesRead);
            if (string.IsNullOrEmpty(requestString))
            {
                ExecutionContext.Current.Logger.LogWarning("Empty request received");
                return null;
            }

            Request request = new Request(requestString);
            return request;
        }

        public async static Task SendResponseAsync(ISocketWrapper client, HttpResponse response, Request request)
        {
            try
            {
                if (response == null)
                    return;
                byte[] msg = Encoding.UTF8.GetBytes(response.ToString());
                ExecutionContext.Current.Logger.LogDebug("Reponse");
                ExecutionContext.Current.Logger.LogDebug(response.ToString());

                await client.SendAsync(msg, SocketFlags.None);

                if (response.Body != null)
                {
                    response.Body.Position = 0; // Assurez-vous que le flux est au début
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = response.Body.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await client.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                    }
                }
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError("Error sending response: " + ex.Message);
            }


        }
    }
}
