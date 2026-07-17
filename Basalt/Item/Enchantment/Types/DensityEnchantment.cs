namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Mace deals more damage per block fallen. +0.5 per level per block.
/// </summary>
public sealed class DensityEnchantment() : EnchantmentType("density", 39, 5)
{
    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx)
    {
        // TODO: Calculate bonus based on fall distance * level * 0.5.
    }
}
