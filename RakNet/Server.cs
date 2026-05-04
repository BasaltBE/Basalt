using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Basalt.RakNet
{
    public class NetworkServer
    {
        public readonly ArrayPool<byte> FramesPool = ArrayPool<byte>.Create(
            maxArrayLength: 2048,
            maxArraysPerBucket: 4096
        );
        public event Action<NetworkConnection>? OnConnected;
        public event Action<NetworkConnection>? OnDisconnected;
        public event Action<NetworkConnection, ReadOnlyMemory<byte>>? OnMessage;

        public void RecieveFrom(EndPoint endpoint, ReadOnlySpan<byte> message)
        {
            
        }
    }
}

