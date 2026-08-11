namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.RakNet;

using BedrockProtocol.Packets;

public static class MobEquipment {
    public static void Handle(Server server, NetworkConnection connection, MobEquipmentPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.TargetRuntimeID.Value != 0 && packet.TargetRuntimeID.Value != player.RuntimeId) {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        if (packet.Slot >= 9) {
            return;
        }

        inventory.SetHeldItem(packet.Slot);
        packet.TargetRuntimeID.Value = player.RuntimeId;
        packet.Item = inventory.GetHeldItem()?.ToNetworkStackDescriptor() ?? new();

        player.Dimension?.Broadcast(packet, new BroadcastOptions {
            Center = player.Position,
            Except = [player],
            Radius = 64,
        });
    }
}
