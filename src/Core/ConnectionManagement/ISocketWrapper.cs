using System.Net.Sockets;

namespace PandApache3.src.Core.ConnectionManagement
{

    public interface ISocketWrapper
    {
        int Receive(byte[] buffer);
        public void Dispose();

        public bool Connected { get; }
        public Task<int> SendAsync(byte[] buffer, SocketFlags socketFlags);

        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags);
    }
}
