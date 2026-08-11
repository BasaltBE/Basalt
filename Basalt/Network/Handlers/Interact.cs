namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Events;
using Basalt.Core.Item.Traits.Types;
using Basalt.RakNet;

using BedrockProtocol.Packets;
using BedrockProtocol.Enums;
using BedrockProtocol.Types;

public static class Interact {
    public static void Handle(Server server, NetworkConnection connection, InteractPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.Action == InteractPacketPayloadAction.StopRiding) {
            EntityRidingTrait? riding = player.GetTrait<EntityRidingTrait>();
            if (riding is not null) {
                EntityRideableTrait? rideable = riding.Vehicle.GetTrait<EntityRideableTrait>();
                rideable?.RemoveRider(player);
            }
            return;
        }

        if (packet.Action  == InteractPacketPayloadAction.OpenInventory) {
            EntityInventoryTrait? playerInventory = player.GetTrait<EntityInventoryTrait>();
            if (playerInventory is null) {
                return;
            }

            playerInventory.Show(player);
            return;
        }

        if (packet.Action == InteractPacketPayloadAction.InteractUpdate) {
            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is null) {
                return;
            }

            var heldItem = inventory.GetHeldItem();
            if (heldItem is null || player.Dimension is null) {
                return;
            }

            foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities) {
                if (entity.RuntimeId != packet.TargetRuntimeID.Value) {
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










