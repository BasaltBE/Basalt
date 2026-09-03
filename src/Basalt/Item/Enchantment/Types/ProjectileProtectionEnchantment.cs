namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Reduces projectile damage. +2 protection points per level against projectiles.
/// </summary>
public sealed class ProjectileProtectionEnchantment() : EnchantmentType("projectile_protection", 4, 4) {
    public override void OnHurt(int level, HurtEnchantmentContext ctx) {
        if (ctx.Source == DamageSource.Projectile) {
            ctx.DamageReduction += level * 0.08f;
        }
    }
}
