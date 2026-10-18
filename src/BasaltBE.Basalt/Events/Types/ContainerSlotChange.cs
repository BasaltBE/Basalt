using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Events.Types;

public readonly struct ContainerSlotChange(Container container, int slot, ItemStack? previous, ItemStack? item) {
    public readonly Container Container = container;
    public readonly int Slot = slot;
    public readonly ItemType? PreviousType = previous?.Type;
    public readonly ushort PreviousAmount = previous?.StackSize ?? 0;
    public readonly uint PreviousMetadata = previous?.Metadata ?? 0;
    public readonly ItemType? ItemType = item?.Type;
    public readonly ushort Amount = item?.StackSize ?? 0;
    public readonly uint Metadata = item?.Metadata ?? 0;
}
