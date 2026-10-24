using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Items.Enchantments;

internal static class EnchantmentPalette {
    public static readonly Enchantment Protection = new(
        "minecraft:protection", 4, static item => item.ArmorSlot is not null, 0
    );
    public static readonly Enchantment Efficiency = new(
        "minecraft:efficiency", 5, static item => item.ToolType != ToolType.None, 15
    );
    public static readonly Enchantment SilkTouch = new(
        "minecraft:silk_touch", 1, static item => item.ToolType != ToolType.None, 16, "minecraft:fortune"
    );
    public static readonly Enchantment Unbreaking = new(
        "minecraft:unbreaking", 3, static item => item.MaxDurability > 0, 17
    );
    public static readonly Enchantment Fortune = new(
        "minecraft:fortune", 3, static item => item.ToolType != ToolType.None, 18, "minecraft:silk_touch"
    );

    private static readonly Enchantment[] enchantments = [
        Protection,
        Efficiency,
        SilkTouch,
        Unbreaking,
        Fortune
    ];

    public static ReadOnlySpan<Enchantment> Enchantments => enchantments;

    public static Enchantment? Find(string identifier) {
        foreach (Enchantment enchantment in enchantments)
            if (string.Equals(enchantment.Identifier, identifier, StringComparison.Ordinal))
                return enchantment;
        return null;
    }
}
