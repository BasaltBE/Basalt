using Basalt.Core;
using Basalt.Entity.Traits;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class Interact
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        InteractPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        if (packet.ActionType == InteractActionType.OpenInventory)
        {
            EntityInventoryTrait? playerInventory = player.GetTrait<EntityInventoryTrait>();
            if (playerInventory is null)
            {
                return;
            }

            if (player.Dimension is null)
            {
                playerInventory.Container.Show(player);
                return;
            }

            Entity.Entity? target = player.Dimension.Entities
                .FirstOrDefault(entity => entity.RuntimeId == packet.TargetEntityRuntimeId);

            if (target is not null)
            {
                EntityInventoryTrait? targetInventory = target.GetTrait<EntityInventoryTrait>();
                if (targetInventory is not null)
                {
                    targetInventory.Container.Show(player);
                    return;
                }
            }

            playerInventory.Container.Show(player);
        }
    }
}
