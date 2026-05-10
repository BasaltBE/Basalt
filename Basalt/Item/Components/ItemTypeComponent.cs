using Basalt.Protocol.Nbt;

namespace Basalt.Item.Components;

public abstract class ItemTypeComponent
{
    public static string Identifier => string.Empty;

    protected ItemType Type { get; }
    protected CompoundTag Component { get; }

    protected ItemTypeComponent(ItemType type, CompoundTag component)
    {
        Type = type;
        Component = component;
    }
}
