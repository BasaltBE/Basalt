namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Mace reduces armor effectiveness. -15% armor per level.
/// </summary>
public sealed class BreachEnchantment() : EnchantmentType("breach", 40, 4)
{
    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx)
    {
        // TODO: Reduce target's effective armor by level * 15%.
    }
}
