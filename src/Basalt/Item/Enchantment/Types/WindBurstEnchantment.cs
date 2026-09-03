namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Mace launches player upward on hit, scaling with level.
/// </summary>
public sealed class WindBurstEnchantment() : EnchantmentType("wind_burst", 38, 3) {
    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) {
        // TODO: Apply upward velocity to player after landing a hit.
    }
}
