
using System.Net;

namespace Basalt.RakNet
{
    public abstract class NetworkConnection
    {
        public abstract void SendMessage(Span<byte> raw);
    }
}
