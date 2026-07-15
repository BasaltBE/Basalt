namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Extends underwater breathing time. +15 seconds per level.
/// </summary>
public sealed class RespirationEnchantment() : EnchantmentType("respiration", 6, 3)
{
  public int GetExtraBreathTicks(int level) => level * 300;
}
