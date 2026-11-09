using System.Collections.Concurrent;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerInventoryTrait : EntityTrait {
    private readonly Dictionary<(Container Container, int Slot), SlotPrediction> slotPredictions = [];
    private readonly ConcurrentQueue<(
        InventoryTransactionData Transaction,
        int LegacyRequestId,
        LegacySetSlot[]? LegacySetItemSlots
    )> transactions = new();
    private int pendingTransactions;

    public readonly EntityContainer Inventory;
    public readonly EntityContainer Cursor;
    public readonly EntityContainer CraftingGrid;
    public byte SelectedSlot;

    internal void QueueTransaction(
        InventoryTransactionData transaction,
        int legacyRequestId,
        LegacySetSlot[]? legacySetItemSlots = null
    ) {
        if (Interlocked.Increment(ref pendingTransactions) > 64) {
            Interlocked.Decrement(ref pendingTransactions);
            return;
        }
        transactions.Enqueue((transaction, legacyRequestId, legacySetItemSlots));
    }

    public override void Tick() {
        Player player = (Player)Entity;
        for (int i = 0; i < 64 && transactions.TryDequeue(out var pending); i++) {
            Interlocked.Decrement(ref pendingTransactions);
            try {
                InventoryTransactionHandler.ProcessTransaction(
                    player,
                    pending.Transaction,
                    pending.LegacyRequestId,
                    pending.LegacySetItemSlots
                );
            } catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                player.Network.RemovePlayer(player.Peer);
                return;
            }
        }
    }

    public override void OnDespawn() {
        while (transactions.TryDequeue(out _))
            Interlocked.Decrement(ref pendingTransactions);
    }

    internal bool MatchesItem(int slot, NetworkItemStackDescriptor descriptor, out ItemStack? item) {
        item = null;
        if ((uint)slot >= Inventory.ContainerSize) return false;

        item = Inventory.GetItem(slot);
        if (item is null || item.StackSize == 0 || item.Type.NetworkId == 0) {
            item = null;
            return descriptor.Id == 0 && descriptor.StackSize == 0;
        }

        return descriptor.Id == item.Type.NetworkId && descriptor.StackSize == item.StackSize &&
            descriptor.AuxValue == item.Metadata &&
            (descriptor.NetIdVariant is not int netId || netId == item.NetId ||
            MatchesClientRequestNetId(Inventory, slot, netId, item));
    }

    public PlayerInventoryTrait(Entity entity) : base(entity) {
        Player player = entity as Player
            ?? throw new ArgumentException("Player inventory traits require a player entity.", nameof(entity));
        Inventory = new EntityContainer(player, 36);
        Cursor = new EntityContainer(
            player,
            1,
            ContainerId.PlayerOnlyUi,
            ContainerEnumName.Cursor,
            ContainerType.Inventory,
            dynamicId: 0
        );
        CraftingGrid = new EntityContainer(
            player,
            4,
            ContainerId.PlayerOnlyUi,
            ContainerEnumName.CraftingInput,
            ContainerType.Inventory,
            slotOffset: 28
        );
    }

    public override void OnSpawn() {
        Player player = (Player)Entity;
        Inventory.AddPersistent(player);
        Cursor.AddPersistent(player);
        CraftingGrid.AddPersistent(player);
        player.SendPacket(new MobEquipmentPacket {
            TargetRuntimeId = player.RuntimeId,
            Item = Inventory.GetItem(SelectedSlot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
            Slot = SelectedSlot,
            SelectedSlot = SelectedSlot,
            ContainerId = (byte)ContainerId.Inventory
        });
    }

    public override void Read(CompoundTag entityTag) {
        SelectedSlot = Math.Min((byte)8, entityTag.Get<ByteTag>("SelectedSlot")?.Value ?? 0);
        if (entityTag.Get<ListTag>("Inventory") is not { } items) return;

        foreach (Tag entry in items.Value) {
            if (entry is not CompoundTag item ||
                !item.TryGet("Slot", out ByteTag? slot) ||
                !item.TryGet("Name", out StringTag? name) ||
                !item.TryGet("Count", out ByteTag? count) ||
                slot!.Value >= Inventory.ContainerSize ||
                ItemPalette.Find(name!.Value) is not { } type ||
                count!.Value == 0)
                continue;

            uint metadata = item.Get<ShortTag>("Damage") is { } damage
                ? unchecked((ushort)damage.Value)
                : 0U;
            Inventory.LoadItem(slot.Value, new ItemStack(type, count.Value, metadata, item.Get<CompoundTag>("tag")));
        }
    }

    public override void Write(CompoundTag entityTag) {
        entityTag.Add("SelectedSlot", new ByteTag { Value = SelectedSlot });
        ListTag items = new() { ElementType = TagType.Compound };
        for (int slot = 0; slot < Inventory.ContainerSize; slot++) {
            if (Inventory.GetItem(slot) is not { } stack) continue;

            CompoundTag item = new();
            item.Add("Name", new StringTag { Value = stack.Type.Identifier });
            item.Add("Count", new ByteTag { Value = checked((byte)stack.StackSize) });
            item.Add("Damage", new ShortTag { Value = unchecked((short)stack.Metadata) });
            item.Add("Slot", new ByteTag { Value = checked((byte)slot) });
            if (stack.Nbt is not null) item.Add("tag", stack.Clone().Nbt!);
            items.Value.Add(item);
        }
        entityTag.Add("Inventory", items);
    }

    internal bool MatchesClientRequestNetId(Container container, int slot, int clientRequestNetId, ItemStack? item) =>
        slotPredictions.TryGetValue((container, slot), out SlotPrediction prediction) &&
        prediction.ClientRequestNetId == clientRequestNetId &&
        prediction.ItemNetId == item?.NetId;

    internal void RememberClientRequestNetId(Container container, int slot, int clientRequestNetId, ItemStack? item) =>
        slotPredictions[(container, slot)] = new SlotPrediction(clientRequestNetId, item?.NetId);

    private readonly record struct SlotPrediction(int ClientRequestNetId, int? ItemNetId);
}
