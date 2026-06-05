namespace Basalt.Server.Network.Handlers;

using Basalt.Server;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class ClientCacheStatus
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ClientCacheStatusPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ClientCacheStatusPacket)Protocol.Io.Packet.Deserialize(reader);
    }
}










