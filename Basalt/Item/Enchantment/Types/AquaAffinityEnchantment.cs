namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Removes underwater mining speed penalty.
/// </summary>
public sealed class AquaAffinityEnchantment() : EnchantmentType("aqua_affinity", 8, 1)
{
    public override float GetMiningSpeedBonus(int level) => 0f;
}
