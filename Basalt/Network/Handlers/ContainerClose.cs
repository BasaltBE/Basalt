using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ContainerClose
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ContainerClosePacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        if (player.OpenedContainer is null)
        {
            return;
        }

        player.OpenedContainer.Close(player, sendPacket: false, serverSide: false);
    }
}
