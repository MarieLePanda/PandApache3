using System.Net.Sockets;

namespace PandApache3.src.Core.ConnectionManagement
{
    public class SocketWrapper : ISocketWrapper
    {
        private readonly Socket _socket;

        public SocketWrapper(Socket socket)
        {
            _socket = socket;
        }

        public int Receive(byte[] buffer)
        {
            return _socket.Receive(buffer);
        }

        public bool Connected {  get { return _socket.Connected; } }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public Task<int> SendAsync(byte[] buffer, SocketFlags socketFlags)
        {
            return _socket.SendAsync(buffer, socketFlags);
        }

        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return _socket.SendAsync(buffer, socketFlags);
        }
    }
}
