using Basalt.Binary;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt.Types;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class BlockPickRequestHandler {
    public void Handle(NethernetPeer peer, Player? player, BlockPickRequestPacket packet) {
        if (player is null)
            throw new InvalidDataException("Block pick request arrived before player login.");
        if (!player.Spawned || player.Disposed ||
            packet.Position.Y is < -64 or >= 320 ||
            player.Dimension.GetChunk(packet.Position.X >> 4, packet.Position.Z >> 4) is not { } chunk ||
            player.GetTrait<PlayerChunkRenderingTrait>()?.Viewing(packet.Position.X >> 4, packet.Position.Z >> 4) != true)
            return;

        float dx = player.Position.X - packet.Position.X - 0.5f;
        float dy = player.Position.Y + player.HeadOffset - packet.Position.Y - 0.5f;
        float dz = player.Position.Z - packet.Position.Z - 0.5f;
        if (dx * dx + dy * dy + dz * dz > 32 * 32)
            return;

        BlockPermutation block = chunk.GetPermutation(packet.Position.X & 15, packet.Position.Y, packet.Position.Z & 15);
        if (block.Identifier == "minecraft:air")
            return;

        ItemType? type = ItemPalette.Find(block.Identifier);
        if (type?.Catalog is null) {
            type = null;
            if (player.Dimension.World is { } world) {
                foreach (var drop in world.LootTables.Get(block.Identifier)) {
                    ItemType? dropType = ItemPalette.Find(drop.Identifier);
                    if (dropType?.Catalog is null)
                        continue;

                    type = dropType;
                    break;
                }
            }
        }

        if (type is null || type.NetworkId == 0 || type.MaxAmount is <= 0 or > ushort.MaxValue)
            return;

        ItemStack item = new(type, checked((ushort)type.MaxAmount));
        if (packet.WithData && player.Dimension.GetBlockActor(chunk, packet.Position.X & 15, packet.Position.Y, packet.Position.Z & 15) is { } actor) {
            using BinaryStream stream = BinaryStream.Rent(256);
            BinaryWriter writer = stream.GetWriter();
            BasaltBE.Nbt.Nbt.Write(actor.Nbt, ref writer);
            int offset = 0;
            if (BasaltBE.Nbt.Nbt.Read(stream.Buffer.Span[..stream.Offset], ref offset) is CompoundTag pickedData) {
                pickedData.Remove("id");
                pickedData.Remove("x");
                pickedData.Remove("y");
                pickedData.Remove("z");
                if (pickedData.Tags.Count > 0)
                    item.Nbt = pickedData;
            }
        }

        if (player.GetTrait<PlayerInventoryTrait>() is not { } inventory)
            return;

        lock (player.Sync) {
            if (!player.Spawned || player.Disposed)
                return;

            int matchingSlot = -1;
            for (int slot = 0; slot < inventory.Inventory.ContainerSize; slot++) {
                if (inventory.Inventory.GetItem(slot)?.CanStackWith(item) == true) {
                    matchingSlot = slot;
                    break;
                }
            }

            if (matchingSlot < 0 && player.GameType != GameType.Creative)
                return;

            if (matchingSlot >= 0) {
                if (matchingSlot < 9)
                    inventory.SelectedSlot = (byte)matchingSlot;
                else
                    inventory.Inventory.Swap(matchingSlot, inventory.SelectedSlot);
            }
            else {
                int emptyHotbar = -1;
                for (int slot = 0; slot < 9; slot++) {
                    if (inventory.Inventory.GetItem(slot) is null) {
                        emptyHotbar = slot;
                        break;
                    }
                }

                if (emptyHotbar >= 0) {
                    inventory.SelectedSlot = (byte)emptyHotbar;
                    inventory.Inventory.SetItem(emptyHotbar, item);
                }
                else {
                    int emptyInventory = -1;
                    for (int slot = 9; slot < inventory.Inventory.ContainerSize; slot++) {
                        if (inventory.Inventory.GetItem(slot) is null) {
                            emptyInventory = slot;
                            break;
                        }
                    }

                    if (emptyInventory >= 0)
                        inventory.Inventory.Swap(emptyInventory, inventory.SelectedSlot);
                    inventory.Inventory.SetItem(inventory.SelectedSlot, item);
                }
            }

            ItemStack? selectedItem = inventory.Inventory.GetItem(inventory.SelectedSlot);
            MobEquipmentPacket equipment = new() {
                TargetRuntimeId = player.RuntimeId,
                Item = selectedItem?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
                Slot = inventory.SelectedSlot,
                SelectedSlot = inventory.SelectedSlot,
                ContainerId = (byte)ContainerId.Inventory
            };
            player.Network.SendPacket(player, equipment);
            player.Dimension.Broadcast(equipment, player, exclude: player);
        }
    }
}
