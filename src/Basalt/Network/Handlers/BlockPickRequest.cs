namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public static class BlockPickRequest {
    public static void Handle(Server server, NetworkConnection connection, BlockPickRequestPacket packet) {
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
        BlockPickRequestPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player) ||
            !player.Spawned ||
            !player.IsAlive ||
            player.Gamemode != GameType.Creative ||
            player.Dimension is not { } dimension) {
            return;
        }

        float dx = player.Position.X - packet.Position.X - 0.5f;
        float dy = player.Position.Y - packet.Position.Y - 0.5f;
        float dz = player.Position.Z - packet.Position.Z - 0.5f;
        if (dx * dx + dy * dy + dz * dz > 1000f) {
            return;
        }

        Block? block = dimension.GetBlock(packet.Position.X, packet.Position.Y, packet.Position.Z);
        if (block is null || block.Type.Air) {
            return;
        }

        string itemIdentifier = block.Identifier switch {
            var identifier when identifier == BlockIdentifier.RedstoneWire.ToIdentifier() => ItemIdentifier.Redstone.ToIdentifier(),
            var identifier when identifier == BlockIdentifier.PoweredRepeater.ToIdentifier() ||
                identifier == BlockIdentifier.UnpoweredRepeater.ToIdentifier() => ItemIdentifier.Repeater.ToIdentifier(),
            _ => block.Identifier
        };
        ItemType? itemType = ItemType.Get(itemIdentifier);
        if (itemType is null || itemType == ItemType.Air) {
            return;
        }

        ItemStack item = new(itemType);
        if (packet.WithData && dimension.GetLoadedChunk(packet.Position.X >> 4, packet.Position.Z >> 4)
            ?.GetBlockStorage(packet.Position) is { } storage) {
            CompoundTag data = storage.Clone();
            data.Values.Remove("x");
            data.Values.Remove("y");
            data.Values.Remove("z");
            data.Values.Remove("id");
            if (data.Values.Count > 0) {
                item.Storage = data;
            }
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        SelectItem(player, inventory, item);
    }

    private static void SelectItem(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemStack item) {
        int matchingInventorySlot = -1;
        for (int slot = 0; slot < inventory.Container.GetSize(); slot++) {
            ItemStack? existing = inventory.Container.GetItem(slot);
            if (existing is null || !existing.CanStackWith(item)) {
                continue;
            }

            if (slot < 9) {
                inventory.SetHeldItem(slot);
                SendHeldItem(player, inventory, slot);
                return;
            }

            matchingInventorySlot = slot;
            break;
        }

        int selectedSlot = inventory.SelectedSlot;
        if (matchingInventorySlot >= 0) {
            inventory.Container.SwapItems(matchingInventorySlot, selectedSlot);
            SendHeldItem(player, inventory, selectedSlot);
            return;
        }

        for (int slot = 0; slot < 9; slot++) {
            if (inventory.Container.GetItem(slot) is not null) {
                continue;
            }

            inventory.Container.SetItem(slot, item);
            inventory.SetHeldItem(slot);
            SendHeldItem(player, inventory, slot);
            return;
        }

        for (int slot = 9; slot < inventory.Container.GetSize(); slot++) {
            if (inventory.Container.GetItem(slot) is not null) {
                continue;
            }

            ItemStack? held = inventory.Container.GetItem(selectedSlot);
            inventory.Container.SetItem(selectedSlot, item);
            inventory.Container.SetItem(slot, held ?? ItemStack.Empty());
            SendHeldItem(player, inventory, selectedSlot);
            return;
        }

        inventory.Container.SetItem(selectedSlot, item);
        SendHeldItem(player, inventory, selectedSlot);
    }

    private static void SendHeldItem(Player.Player player, EntityInventoryTrait inventory, int slot) {
        player.Send(new MobEquipmentPacket {
            TargetRuntimeId = player.RuntimeId,
            Item = inventory.Container.GetItem(slot)?.ToNetworkStackDescriptor() ?? new(),
            Slot = (byte)slot,
            SelectedSlot = (byte)slot,
            ContainerId = ContainerId.Inventory
        });
    }
}
