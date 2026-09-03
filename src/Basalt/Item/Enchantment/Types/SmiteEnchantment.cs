namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Extra damage to undead mobs. +2.5 per level.
/// </summary>
public sealed class SmiteEnchantment() : EnchantmentType("smite", 10, 5) {
    public override float GetAttackBonus(int level) => level * 2.5f;

    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) {
        // TODO: Check if target is undead before applying.
        ctx.BonusDamage += GetAttackBonus(level);
    }
}
