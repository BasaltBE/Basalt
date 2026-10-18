using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class MobEquipmentHandler {
    public void Handle(NethernetPeer peer, Player? player, MobEquipmentPacket packet) {
        if (player is null)
            throw new InvalidDataException("Equipment arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        lock (player.Sync) {
            if (player.Disposed || !player.Spawned ||
                (packet.TargetRuntimeId != 0 && packet.TargetRuntimeId != player.RuntimeId) ||
                packet.ContainerId != (byte)ContainerId.Inventory || packet.SelectedSlot >= 9)
                return;

            PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
            if (inventory is null || !inventory.MatchesItem(packet.SelectedSlot, packet.Item, out var item))
                return;

            inventory.SelectedSlot = packet.SelectedSlot;
            player.Dimension.Broadcast(new MobEquipmentPacket {
                TargetRuntimeId = player.RuntimeId,
                Item = item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
                Slot = packet.SelectedSlot,
                SelectedSlot = packet.SelectedSlot,
                ContainerId = (byte)ContainerId.Inventory
            }, player, exclude: player);
        }
    }
}
