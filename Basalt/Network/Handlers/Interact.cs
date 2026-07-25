namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;


public static class Interact {
    public static void Handle(Server server, NetworkConnection connection, InteractPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.ActionType == InteractActionType.LeaveVehicle) {
            EntityRidingTrait? riding = player.GetTrait<EntityRidingTrait>();
            if (riding is not null) {
                EntityRideableTrait? rideable = riding.Vehicle.GetTrait<EntityRideableTrait>();
                rideable?.RemoveRider(player);
            }
            return;
        }

        if (packet.ActionType == InteractActionType.OpenInventory) {
            EntityInventoryTrait? playerInventory = player.GetTrait<EntityInventoryTrait>();
            if (playerInventory is null) {
                return;
            }

            playerInventory.Show(player);
            return;
        }

        if (packet.ActionType == InteractActionType.MouseOverEntity) {
            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is null) {
                return;
            }

            var heldItem = inventory.GetHeldItem();
            if (heldItem is null || player.Dimension is null) {
                return;
            }

            foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities) {
                if (entity.RuntimeId != packet.TargetEntityRuntimeId) {
                    continue;
                }

                Vec3f clicked = packet.Position.HasValue && packet.Position.Value is Vec3f value ? value : new Vec3f();
                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(player, entity, 0, player.Location, clicked));
                break;
            }
        }
    }
}










