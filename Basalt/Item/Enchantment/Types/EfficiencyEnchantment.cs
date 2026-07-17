namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases mining speed. level^2 + 1 bonus speed.
/// </summary>
public sealed class EfficiencyEnchantment() : EnchantmentType("efficiency", 15, 5)
{
    public override float GetMiningSpeedBonus(int level) => (level * level) + 1;
}
