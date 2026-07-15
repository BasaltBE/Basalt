namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Reflects damage back to attackers. level * 15% chance to deal 1-4 damage.
/// </summary>
public sealed class ThornsEnchantment() : EnchantmentType("thorns", 5, 3)
{
  public override void OnHurt(int level, HurtEnchantmentContext ctx)
  {
    if (ctx.Source != DamageSource.Melee) return;

    float chance = level * 0.15f;
    if (Random.Shared.NextSingle() < chance)
    {
      ctx.ReflectedDamage += Random.Shared.Next(1, 5);
    }
  }
}
