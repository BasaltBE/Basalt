namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Reduces fire damage. +2 protection points per level against fire.
/// </summary>
public sealed class FireProtectionEnchantment() : EnchantmentType("fire_protection", 1, 4)
{
  public override void OnHurt(int level, HurtEnchantmentContext ctx)
  {
    if (ctx.Source == DamageSource.Fire)
    {
      ctx.DamageReduction += level * 0.08f;
    }
  }
}
