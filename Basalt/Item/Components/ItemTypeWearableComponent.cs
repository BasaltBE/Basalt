namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;

public sealed class ItemTypeWearableComponent : ItemTypeComponent
{
    public new static string Identifier => "minecraft:wearable";

    public ItemTypeWearableComponent(ItemType type, CompoundTag component) : base(type, component)
    {
    }

    public int GetSlot()
    {
        return Component.Get<IntTag>("slot")?.Value ?? -1;
    }
}
