using System;
using System.Net.Sockets;

namespace pandapache.src.ConnectionManagement
{

    public interface ISocketWrapper
    {
        int Receive(byte[] buffer);
        public void Dispose();

        //public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags);

        public Task<int> SendAsync(byte[] buffer, SocketFlags socketFlags);

        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags);
    }
}
