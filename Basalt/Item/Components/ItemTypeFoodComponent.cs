namespace Basalt.Server.Item.Components;

using Basalt.Protocol.Nbt;


public sealed class ItemTypeFoodComponent : ItemTypeComponent
{
    public new static string Identifier => "minecraft:food";

    public ItemTypeFoodComponent(ItemType type, CompoundTag component) : base(type, component)
    {
    }

    public int GetNutrition()
    {
        return Component.Get<IntTag>("nutrition")?.Value ?? 0;
    }

    public float GetSaturationModifier()
    {
        return Component.Get<FloatTag>("saturation_modifier")?.Value
               ?? Component.Get<FloatTag>("saturationModifier")?.Value
               ?? 0f;
    }

    public bool CanAlwaysEat()
    {
        return (Component.Get<ByteTag>("can_always_eat")?.Value
                ?? Component.Get<ByteTag>("canAlwaysEat")?.Value
                ?? 0) != 0;
    }

    public string GetUsingConvertsTo()
    {
        return Component.Get<StringTag>("using_converts_to")?.Value
               ?? Component.Get<StringTag>("usingConvertsTo")?.Value
               ?? string.Empty;
    }
}
