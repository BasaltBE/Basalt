namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases melee damage. +1.25 on level 1, +0.75 per additional level.
/// </summary>
public sealed class SharpnessEnchantment() : EnchantmentType("sharpness", 9, 5) {
    public override float GetAttackBonus(int level) => 1.25f + (level - 1) * 0.75f;

    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) {
        ctx.BonusDamage += GetAttackBonus(level);
    }
}
