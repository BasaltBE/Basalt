namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases block drops. Actual drop multiplication is handled by the block drop system
/// using the context's drop list, this just signals fortune is active.
/// </summary>
public sealed class FortuneEnchantment() : EnchantmentType("fortune", 18, 3)
{
    public override void OnBlockBreak(int level, BlockBreakEnchantmentContext ctx)
    {
        // Drop multiplication logic depends on the block type.
        // TODO: Check if the block is affected by fortune / IIRC ITs only ores??
    }
}
