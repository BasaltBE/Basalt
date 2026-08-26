namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Events;
using Basalt.Core.Item.Traits.Types;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;

public static class Interact {
    public static void Handle(Server server, NetworkConnection connection, InteractPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) ||
            player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        InteractPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        if (packet.Action == InteractAction.StopRiding) {
            EntityRidingTrait? riding = player.GetTrait<EntityRidingTrait>();
            if (riding is not null) {
                EntityRideableTrait? rideable = riding.Vehicle.GetTrait<EntityRideableTrait>();
                rideable?.RemoveRider(player);
            }
            return;
        }

        if (packet.Action  == InteractAction.OpenInventory) {
            EntityInventoryTrait? playerInventory = player.GetTrait<EntityInventoryTrait>();
            if (playerInventory is null) {
                return;
            }

            playerInventory.Show(player);
            return;
        }

        if (packet.Action == InteractAction.InteractUpdate) {
            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is null) {
                return;
            }

            var heldItem = inventory.GetHeldItem();
            if (heldItem is null || player.Dimension is null) {
                return;
            }

            foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities) {
                if (entity.RuntimeId != packet.TargetRuntimeId) {
                    continue;
                }

                Vec3 clicked = packet.Position ?? new Vec3();
                PlayerUseItemSignal signal = new(player, heldItem);
                server.Emit(signal);
                if (!signal.Emit()) {
                    break;
                }

                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(player, entity, 0, player.Location, clicked));
                break;
            }
        }
    }
}










