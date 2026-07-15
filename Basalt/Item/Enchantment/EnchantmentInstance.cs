namespace Basalt.Core.Item.Enchantment;

/// <summary>
/// Represents a single enchantment applied to an item (type + level).
/// </summary>
public readonly struct EnchantmentInstance
{
  public EnchantmentType Type { get; }
  public int Level { get; }

  public EnchantmentInstance(EnchantmentType type, int level)
  {
    Type = type;
    Level = Math.Clamp(level, 1, type.MaxLevel);
  }

  public static EnchantmentInstance? Create(int id, int level)
  {
    EnchantmentType? type = EnchantmentType.Get(id);
    if (type is null) return null;
    return new EnchantmentInstance(type, level);
  }

  public static EnchantmentInstance? Create(string identifier, int level)
  {
    EnchantmentType? type = EnchantmentType.Get(identifier);
    if (type is null) return null;
    return new EnchantmentInstance(type, level);
  }
}
