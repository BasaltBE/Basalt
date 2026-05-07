using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class Interact
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        InteractPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.ContainsKey(connection))
        {
            return;
        }
    }
}
