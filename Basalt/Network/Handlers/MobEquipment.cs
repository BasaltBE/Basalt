namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class MobEquipment {
    public static void Handle(Server server, NetworkConnection connection, MobEquipmentPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.EntityRuntimeId != 0 && packet.EntityRuntimeId != player.RuntimeId) {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        if (packet.HotBarSlot >= 9) {
            return;
        }

        inventory.SetHeldItem(packet.HotBarSlot);
        packet.EntityRuntimeId = player.RuntimeId;
        packet.NewItem = inventory.GetHeldItem()?.ToNetworkStackDescriptor() ?? new();

        player.Dimension?.Broadcast(packet, new BroadcastOptions {
            Center = player.Position,
            Except = [player],
            Radius = 64,
        });
    }
}
