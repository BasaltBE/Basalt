namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Chance to prevent durability loss. Probability = 1 / (level + 1).
/// </summary>
public sealed class UnbreakingEnchantment() : EnchantmentType("unbreaking", 17, 3)
{
    public override void OnBlockBreak(int level, BlockBreakEnchantmentContext ctx)
    {
        if (ShouldPreventDamage(level))
        {
            ctx.PreventDurabilityLoss = true;
        }
    }

    private static bool ShouldPreventDamage(int level)
    {
        return Random.Shared.Next(level + 1) > 0;
    }
}
