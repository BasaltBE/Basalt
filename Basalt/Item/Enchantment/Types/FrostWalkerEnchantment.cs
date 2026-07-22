namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Turns water into frosted ice when walking near it. Radius = level + 2.
/// </summary>
public sealed class FrostWalkerEnchantment() : EnchantmentType("frost_walker", 25, 2) {
    public override void OnTick(int level, TickEnchantmentContext ctx) {
        if (ctx.Slot != EquipmentSlot.Feet) return;
        // TODO: Freeze nearby water blocks within radius (level + 2).
    }
}
