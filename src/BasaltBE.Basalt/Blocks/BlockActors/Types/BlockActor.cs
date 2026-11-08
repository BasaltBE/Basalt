using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors.Types;

public abstract class BlockActor(BlockActorContext context) {
    public readonly Dimension Dimension = context.Dimension;
    public readonly BlockPos Position = context.Position;
    public readonly string Identifier = context.Identifier;
    public readonly BlockPermutation Permutation = context.Permutation;
    public readonly CompoundTag Nbt = context.Nbt;
    public virtual string ActorIdentifier => Identifier;
    public virtual Container? Inventory => null;

    /// <summary>
    /// Whether an item can enter this inventory through a certain face
    /// </summary>
    public virtual bool CanInsert(int slot, ItemStack item, byte face) => Inventory?.CanContain(slot, item) == true;

    /// <summary>
    /// Whether an item can leave this inventory through a certain face
    /// </summary>
    public virtual bool CanExtract(int slot, ItemStack item, byte face) => true;

    internal ItemStack? InsertItem(ItemStack item, byte face) {
        ArgumentNullException.ThrowIfNull(item);
        if (Inventory is not { } inventory) return item;

        for (int slot = 0; slot < inventory.ContainerSize && item.StackSize > 0; slot++) {
            // Can not add the item or the slow have that item
            if (!CanInsert(slot, item, face) || inventory.GetItem(slot) is not { } existing ||
                !existing.CanStackWith(item) || existing.StackSize >= existing.Type.MaxAmount)
                continue;

            ushort moved = (ushort)Math.Min(item.StackSize, existing.Type.MaxAmount - existing.StackSize);
            existing.IncrementStack(moved);
            item.DecrementStack(moved);
        }

        for (int slot = 0; slot < inventory.ContainerSize && item.StackSize > 0; slot++) {
            // Can not add the item or the slow have that item
            if (!CanInsert(slot, item, face) || inventory.GetItem(slot) is not null)
                continue;

            ushort moved = (ushort)Math.Min(item.StackSize, item.Type.MaxAmount);
            inventory.SetItem(slot, item.Clone(moved));
            item.DecrementStack(moved);
        }

        return item.StackSize == 0 ? null : item;
    }

    public abstract void OnRead(CompoundTag nbt);
    public abstract void OnWrite(CompoundTag nbt);
    public virtual void OnPlace(ItemStack? item, byte face) { }
    public virtual void OnInteract(Player player) { }
    public virtual bool CanRemove() => true;
    public virtual void OnBreak(Player player) { }
    public virtual void OnRemove() { }
    public virtual void Tick() { }
    public virtual bool DropsInCreative => false;
    public virtual ItemStack? CreateDrop() => null;

    public virtual CompoundTag CreateItemData() {
        CompoundTag itemData = new();
        OnWrite(itemData);

        itemData.Remove("id");
        itemData.Remove("x");
        itemData.Remove("y");
        itemData.Remove("z");
        return itemData;
    }

    protected static void ReadItems(CompoundTag nbt, Container container) {
        if (nbt.Get<ListTag>("Items") is not { } items)
            return;

        foreach (Tag entry in items.Value) {
            if (entry is not CompoundTag item ||
                item.Get<ByteTag>("Slot") is not { } slot ||
                item.Get<StringTag>("Name") is not { } name ||
                item.Get<ByteTag>("Count") is not { } count)
                continue;

            if (slot.Value >= container.ContainerSize ||
                count.Value == 0 ||
                ItemPalette.Find(name.Value) is not { } type)
                continue;

            uint metadata = item.Get<ShortTag>("Damage") is { Value: var damage } ? unchecked((ushort)damage) : 0U;
            container.LoadItem(slot.Value, new ItemStack(type, count.Value, metadata, item.Get<CompoundTag>("tag")));
        }
    }

    protected static ListTag WriteItems(Container container, bool includePickupState = false) {
        ListTag items = new() { ElementType = TagType.Compound };
        for (int slot = 0; slot < container.ContainerSize; slot++) {
            if (container.GetItem(slot) is not { } stack) continue;

            CompoundTag item = new();
            item.Add("Name", new StringTag { Value = stack.Type.Identifier });
            item.Add("Count", new ByteTag { Value = checked((byte)stack.StackSize) });
            item.Add("Damage", new ShortTag { Value = unchecked((short)stack.Metadata) });
            item.Add("Slot", new ByteTag { Value = checked((byte)slot) });

            if (includePickupState) item.Add("WasPickedUp", new ByteTag { Value = 0 });
            if (stack.Nbt is { } itemNbt) item.Add("tag", itemNbt);

            items.Value.Add(item);
        }

        return items;
    }

    public void Update() {
        CompoundTag update = new();
        update.Add("id", new StringTag { Value = ActorIdentifier });
        update.Add("x", new IntTag { Value = Position.X });
        update.Add("y", new IntTag { Value = Position.Y });
        update.Add("z", new IntTag { Value = Position.Z });

        OnWrite(update);

        foreach ((string name, Tag tag) in update.Tags) Nbt.Add(name, tag);
        MarkDirty();

        Dimension.Broadcast(new BlockActorDataPacket {
            Position = Position,
            ActorData = update
        }, Position.X >> 4, Position.Z >> 4);
    }

    protected void MarkDirty() => Dimension.GetChunk(Position.X >> 4, Position.Z >> 4)?.MarkDirty();
}
