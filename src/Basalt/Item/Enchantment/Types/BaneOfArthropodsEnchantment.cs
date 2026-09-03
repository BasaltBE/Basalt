namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Extra damage to arthropod mobs. +2.5 per level.
/// </summary>
public sealed class BaneOfArthropodsEnchantment() : EnchantmentType("bane_of_arthropods", 11, 5) {
    public override float GetAttackBonus(int level) => level * 2.5f;

    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) {
        // TODO: Check if target is arthropod before applying.
        ctx.BonusDamage += GetAttackBonus(level);
    }
}
