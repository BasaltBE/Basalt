using Basalt.Core;
using Basalt.Entity.Traits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class InventoryContent
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        InventoryContentPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        if (packet.WindowId > int.MaxValue || !player.TryGetOpenContainer((int)packet.WindowId, out Containers.Container? container))
        {
            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is null || packet.WindowId != (uint)(inventory.Container.Identifier ?? 0))
            {
                return;
            }

            inventory.Container.Update();
            return;
        }

        container!.Update();
    }
}
