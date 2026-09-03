namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases mob loot drops. Level passed through context for drop calculation.
/// </summary>
public sealed class LootingEnchantment() : EnchantmentType("looting", 14, 3) {
    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) {
        ctx.LootingLevel += level;
    }
}
