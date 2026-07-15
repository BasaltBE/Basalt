namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Reduces fall damage. +3 protection points per level against fall damage.
/// </summary>
public sealed class FeatherFallingEnchantment() : EnchantmentType("feather_falling", 2, 4)
{
  public override void OnHurt(int level, HurtEnchantmentContext ctx)
  {
    if (ctx.Source == DamageSource.Fall)
    {
      ctx.DamageReduction += level * 0.12f;
    }
  }
}
