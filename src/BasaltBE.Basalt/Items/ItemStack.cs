using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Items;

public sealed class ItemStack {
    public ItemType Type;
    public ushort StackSize;
    public uint Metadata;
    public CompoundTag? Nbt;

    public ItemStack(ItemType type, ushort stackSize = 1, uint metadata = 0, CompoundTag? nbt = null) {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
        StackSize = stackSize;
        Metadata = metadata;
        Nbt = nbt;
    }

    public ItemStack(string identifier, ushort stackSize = 1, uint metadata = 0, CompoundTag? nbt = null)
        : this(
            ItemPalette.Find(identifier) ?? throw new KeyNotFoundException($"Item type '{identifier}' was not found."),
            stackSize,
            metadata,
            nbt
        ) {
    }
}
