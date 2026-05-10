using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Core;
using Basalt.Entity.Traits;

namespace Basalt.Network.Handlers;

public static class ContainerClose
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ContainerClosePacket packet = new();
        packet.Deserialize(packetBuffer);

        if (server.Players.TryGetValue(connection, out Player? player))
        {
            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is not null && packet.WindowId == (byte)(inventory.Container.Identifier ?? 0))
            {
                inventory.Container.RemoveOccupant(player);
            }
        }

        ContainerClosePacket response = new()
        {
            WindowId = packet.WindowId,
            ContainerType = packet.ContainerType,
            ServerSide = false
        };
        server.Network.SendPacket(connection, response);
    }
}
