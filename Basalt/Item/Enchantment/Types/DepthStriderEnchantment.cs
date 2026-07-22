namespace Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Increases underwater movement speed.
/// </summary>
public sealed class DepthStriderEnchantment() : EnchantmentType("depth_strider", 7, 3) {
    public static float GetSpeedMultiplier(int level) => level / 3f;
}
