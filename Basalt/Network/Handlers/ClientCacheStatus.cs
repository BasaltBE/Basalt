using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ClientCacheStatus
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ClientCacheStatusPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet.Deserialize(reader);
    }
}
