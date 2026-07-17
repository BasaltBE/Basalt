namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


/// <summary>
/// Represents the "minecraft:fuel" component that defines fuel burn duration.
/// </summary>
public sealed class ItemTypeFuelComponent : ItemTypeComponent
{
    public new static string Identifier => "minecraft:fuel";

    public ItemTypeFuelComponent(ItemType type, CompoundTag component) : base(type, component)
    {
    }

    /// <summary>
    /// Gets the burn duration in seconds.
    /// </summary>
    public float GetDuration()
    {
        return Component.Get<FloatTag>("duration")?.Value
               ?? Component.Get<IntTag>("duration")?.Value
               ?? 0f;
    }
}
