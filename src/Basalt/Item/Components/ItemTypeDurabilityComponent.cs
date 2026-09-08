namespace Basalt.Core.Item.Components;

using Basalt.BedrockProtocol.NBT;


public sealed class ItemTypeDurabilityComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:durability";

    public ItemTypeDurabilityComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    public int GetMaxDurability() {
        int value = Component.Get<IntTag>("maxDurability")?.Value
            ?? Component.Get<IntTag>("max_durability")?.Value
            ?? 0;

        if (value > 0) return value;

        string? tier = Type.Tags.FirstOrDefault(static tag => tag.EndsWith("_tier", StringComparison.Ordinal));
        if (tier is null) return 0;

        int toolDurability = tier switch {
            "minecraft:wooden_tier" => 59,
            "minecraft:stone_tier" => 131,
            "minecraft:iron_tier" => 250,
            "minecraft:golden_tier" => 32,
            "minecraft:diamond_tier" => 1561,
            "minecraft:netherite_tier" => 2031,
            _ => 0
        };

        return Type.Tags.Any(static tag => tag is "minecraft:is_sword" or "minecraft:is_pickaxe" or
            "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe")
            ? toolDurability
            : 0;
    }

    public (int Min, int Max) GetDamageChance() {
        CompoundTag? chance = Component.Get<CompoundTag>("damageChanceRange")
            ?? Component.Get<CompoundTag>("damage_chance");

        if (chance is null) {
            return (100, 100);
        }

        int min = chance.Get<IntTag>("min")?.Value ?? 100;
        int max = chance.Get<IntTag>("max")?.Value ?? 100;
        return (min, max);
    }
}






