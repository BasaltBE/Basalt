namespace Basalt.Core.Item.Components;

using BedrockProtocol.Nbt;


public sealed class ItemTypeDurabilityComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:durability";

    public ItemTypeDurabilityComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    public int GetMaxDurability() {
        return Component.Get<IntTag>("maxDurability")?.Value
            ?? Component.Get<IntTag>("max_durability")?.Value
            ?? 0;
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






