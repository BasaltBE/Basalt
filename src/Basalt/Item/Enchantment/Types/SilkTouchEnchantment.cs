namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Causes blocks to drop themselves instead of their normal drops.
/// </summary>
public sealed class SilkTouchEnchantment() : EnchantmentType("silk_touch", 16, 1) {
    public override void OnBlockBreak(int level, BlockBreakEnchantmentContext ctx) {
        ctx.SilkTouch = true;
    }
}
