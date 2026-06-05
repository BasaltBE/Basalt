namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


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






