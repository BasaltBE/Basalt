namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases bow damage. +25% per level (base: +50% at level 1).
/// </summary>
public sealed class PowerEnchantment() : EnchantmentType("power", 19, 5) {
    public static float GetDamageMultiplier(int level) => 1f + (level + 1) * 0.25f;
}
