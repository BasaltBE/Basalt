namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;
using Basalt.Core.Containers;

public static class ContainerClose {
    public static void Handle(Server server, NetworkConnection connection, ContainerClosePacket packet) {
        if (server.Players.TryGetValue(connection, out Player.Player? player)) {
            ArgumentNullException.ThrowIfNull(player);

            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is not null && packet.ContainerId == (ContainerId)(inventory.Container.Identifier ?? ContainerId.Inventory)) {
                inventory.Container.RemoveViewer(player, false);
            }
            else if (player.TryGetOpenContainer(packet.ContainerId, out Containers.Container? openContainer) && openContainer is not null) {
                openContainer.RemoveViewer(player, false);
            }
        }

        ContainerClosePacket response = new() {
            ContainerId = packet.ContainerId,
            ContainerType = packet.ContainerType,
            ServerInitiatedClose = false
        };
        server.Network.QueuePacket(connection, response);
    }
}
