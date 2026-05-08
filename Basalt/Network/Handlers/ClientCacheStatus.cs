using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ClientCacheStatus
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ClientCacheStatusPacket packet = new();
        packet.Deserialize(packetBuffer);
    }
}
