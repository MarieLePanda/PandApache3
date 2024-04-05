using System;
using System.Net.Sockets;

namespace pandapache.src.ConnectionManagement
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
