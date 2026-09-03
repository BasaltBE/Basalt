namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases speed on soul sand and soul soil.
/// </summary>
public sealed class SoulSpeedEnchantment() : EnchantmentType("soul_speed", 36, 3) {
    public override void OnTick(int level, TickEnchantmentContext ctx) {
        if (ctx.Slot != EquipmentSlot.Feet) return;
        // TODO: Check if player is on soul sand/soul soil and apply speed.
    }
}
