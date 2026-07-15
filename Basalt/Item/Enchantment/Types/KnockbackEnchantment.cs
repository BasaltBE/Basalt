namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases knockback on hit. Multiplier grows per level.
/// </summary>
public sealed class KnockbackEnchantment() : EnchantmentType("knockback", 12, 2)
{
  public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx)
  {
    ctx.KnockbackMultiplier += level * 0.4f;
  }
}
