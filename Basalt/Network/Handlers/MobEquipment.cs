namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
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

        if (packet.HotBarSlot < 9) {
            inventory.SetHeldItem(packet.HotBarSlot);
        }
    }
}
