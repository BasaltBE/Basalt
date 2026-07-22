namespace Basalt.Core.Item.Enchantment;

/// <summary>
/// A single enchantment applied to an item 
/// </summary>
public readonly struct EnchantmentInstance(EnchantmentType type, int level) {
    public EnchantmentType Type { get; } = type;
    public int Level { get; } = Math.Clamp(level, 1, type.MaxLevel);

    public float GetAttackBonus() => Type.GetAttackBonus(Level);
    public float GetProtectionBonus() => Type.GetProtectionBonus(Level);
    public float GetMiningSpeedBonus() => Type.GetMiningSpeedBonus(Level);

    public static EnchantmentInstance? Create(int id, int level) {
        EnchantmentType? type = EnchantmentType.Get(id);
        if (type is null) return null;
        return new EnchantmentInstance(type, level);
    }

    public static EnchantmentInstance? Create(string identifier, int level) {
        EnchantmentType? type = EnchantmentType.Get(identifier);
        if (type is null) return null;
        return new EnchantmentInstance(type, level);
    }
}
