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
                !item.TryGet("Slot", out ByteTag? slot) ||
                !item.TryGet("Name", out StringTag? name) ||
                !item.TryGet("Count", out ByteTag? count) ||
                slot!.Value >= container.ContainerSize ||
                ItemPalette.Find(name!.Value) is not { } type ||
                count!.Value == 0)
                continue;

            uint metadata = item.Get<ShortTag>("Damage") is { } damage
                ? unchecked((ushort)damage.Value)
                : 0U;
            container.LoadItem(slot.Value, new ItemStack(type, count.Value, metadata, item.Get<CompoundTag>("tag")));
        }
    }

    protected static ListTag WriteItems(Container container) {
        ListTag items = new() { ElementType = TagType.Compound };
        for (int slot = 0; slot < container.ContainerSize; slot++) {
            if (container.GetItem(slot) is not { } stack)
                continue;

            CompoundTag item = new();
            item.Add("Name", new StringTag { Value = stack.Type.Identifier });
            item.Add("Count", new ByteTag { Value = checked((byte)stack.StackSize) });
            item.Add("Damage", new ShortTag { Value = unchecked((short)stack.Metadata) });
            item.Add("Slot", new ByteTag { Value = checked((byte)slot) });
            if (stack.Nbt is { } itemNbt)
                item.Add("tag", itemNbt);
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
        foreach ((string name, Tag tag) in update.Tags)
            Nbt.Add(name, tag);
        MarkDirty();

        Dimension.Broadcast(new BlockActorDataPacket {
            Position = Position,
            ActorData = update
        }, Position.X >> 4, Position.Z >> 4);
    }

    protected void MarkDirty() => Dimension.GetChunk(Position.X >> 4, Position.Z >> 4)?.MarkDirty();
}
