using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class InteractHandler {
    public void Handle(NethernetPeer peer, Player? player, InteractPacket packet) {
        if (player is null)
            throw new InvalidDataException("Interact packet arrived before player login.");
        if (!player.Spawned || player.Disposed)
            return;

        if (packet.Action == InteractAction.OpenInventory)
            player.GetTrait<PlayerInventoryTrait>()?.Inventory.Open(player);
    }
}
