namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// General damage reduction. +1 protection point per level (max 4).
/// Each point reduces damage by 4%.
/// </summary>
public sealed class ProtectionEnchantment() : EnchantmentType("protection", 0, 4)
{
    public override float GetProtectionBonus(int level) => level;

    public override void OnHurt(int level, HurtEnchantmentContext ctx)
    {
        ctx.DamageReduction += level * 0.04f;
    }
}
