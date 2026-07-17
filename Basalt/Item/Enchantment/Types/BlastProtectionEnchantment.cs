namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Reduces explosion damage. +2 protection points per level against explosions.
/// </summary>
public sealed class BlastProtectionEnchantment() : EnchantmentType("blast_protection", 3, 4)
{
    public override void OnHurt(int level, HurtEnchantmentContext ctx)
    {
        if (ctx.Source == DamageSource.Explosion)
        {
            ctx.DamageReduction += level * 0.08f;
        }
    }
}
