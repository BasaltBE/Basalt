namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases sneaking speed. 15% per level (75% at max).
/// </summary>
public sealed class SwiftSneakEnchantment() : EnchantmentType("swift_sneak", 37, 3)
{
    public static float GetSneakSpeedMultiplier(int level) => Math.Min(0.15f * level + 0.30f, 0.75f);
}
