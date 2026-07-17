namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Sets the target on fire. 80 ticks (4s) per level.
/// </summary>
public sealed class FireAspectEnchantment() : EnchantmentType("fire_aspect", 13, 2)
{
    public override void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx)
    {
        ctx.FireTicks += level * 80;
    }
}
