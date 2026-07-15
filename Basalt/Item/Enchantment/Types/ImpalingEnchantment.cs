namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Extra damage to aquatic mobs (trident). +2.5 per level.
/// In Bedrock, also applies to mobs in water or rain.
/// </summary>
public sealed class ImpalingEnchantment() : EnchantmentType("impaling", 29, 5)
{
  public override float GetAttackBonus(int level) => level * 2.5f;

  public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx)
  {
    // TODO: Check if target is aquatic or in water/rain.
    ctx.BonusDamage += GetAttackBonus(level);
  }
}
