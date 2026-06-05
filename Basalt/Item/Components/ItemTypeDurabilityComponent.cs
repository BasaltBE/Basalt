namespace Basalt.Server.Item.Components;

using Basalt.Protocol.Nbt;


public sealed class ItemTypeDurabilityComponent : ItemTypeComponent
{
    public new static string Identifier => "minecraft:durability";

    public ItemTypeDurabilityComponent(ItemType type, CompoundTag component) : base(type, component)
    {
    }

    public int GetMaxDurability()
    {
        return Component.Get<IntTag>("max_durability")?.Value ?? 0;
    }

    public (int Min, int Max) GetDamageChance()
    {
        CompoundTag? chance = Component.Get<CompoundTag>("damage_chance");
        if (chance is null)
        {
            return (0, 0);
        }

        int min = chance.Get<IntTag>("min")?.Value ?? 0;
        int max = chance.Get<IntTag>("max")?.Value ?? 0;
        return (min, max);
    }
}






