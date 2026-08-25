namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Worlds.Dimensions;

using Basalt.BedrockProtocol.Packets;

public static class MobEquipment {
    public static void Handle(Server server, NetworkConnection connection, MobEquipmentPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) ||
            player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(() => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        MobEquipmentPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        if (packet.TargetRuntimeId != 0 && packet.TargetRuntimeId != player.RuntimeId) {
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
        packet.TargetRuntimeId = player.RuntimeId;
        packet.Item = inventory.GetHeldItem()?.ToNetworkStackDescriptor() ?? new();

        player.Dimension?.Broadcast(packet, new BroadcastOptions {
            Center = player.Position,
            Except = [player],
            Radius = 64,
        });
    }
}
