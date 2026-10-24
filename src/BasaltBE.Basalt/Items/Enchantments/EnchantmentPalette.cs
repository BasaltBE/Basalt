using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Items.Enchantments;

internal static class EnchantmentPalette {
    public static readonly Enchantment Protection = new(
        "minecraft:protection", 4, static item => item.ArmorSlot is not null, (short)EnchantType.Protection, 1,
        "minecraft:fire_protection", "minecraft:blast_protection", "minecraft:projectile_protection"
    );
    public static readonly Enchantment Efficiency = new(
        "minecraft:efficiency", 5, static item => item.ToolType != ToolType.None, (short)EnchantType.Efficiency, 1
    );
    public static readonly Enchantment SilkTouch = new(
        "minecraft:silk_touch", 1, static item => item.ToolType != ToolType.None, (short)EnchantType.SilkTouch, 4,
        "minecraft:fortune"
    );
    public static readonly Enchantment Unbreaking = new(
        "minecraft:unbreaking", 3, static item => item.MaxDurability > 0, (short)EnchantType.Unbreaking, 1
    );
    public static readonly Enchantment Fortune = new(
        "minecraft:fortune", 3, static item => item.ToolType != ToolType.None, (short)EnchantType.Fortune, 2,
        "minecraft:silk_touch"
    );
    public static readonly Enchantment AquaAffinity = new(
        "minecraft:aqua_affinity", 1, static item => item.ArmorSlot == ArmorSlot.Head, (short)EnchantType.AquaAffinity, 2
    );
    public static readonly Enchantment BaneOfArthropods = new(
        "minecraft:bane_of_arthropods", 5, IsMeleeWeapon, (short)EnchantType.BaneOfArthropods, 2,
        "minecraft:sharpness", "minecraft:smite"
    );
    public static readonly Enchantment Binding = new(
        "minecraft:binding", 1, IsEnchantable, (short)EnchantType.CurseOfBinding, 8
    );
    public static readonly Enchantment BlastProtection = new(
        "minecraft:blast_protection", 4, static item => item.ArmorSlot is not null, (short)EnchantType.BlastProtection, 2,
        "minecraft:protection", "minecraft:fire_protection", "minecraft:projectile_protection"
    );
    public static readonly Enchantment Breach = new(
        "minecraft:breach", 4, IsMace, (short)EnchantType.Breach, 4, "minecraft:density"
    );
    public static readonly Enchantment Channeling = new(
        "minecraft:channeling", 1, IsTrident, (short)EnchantType.Channeling, 4, "minecraft:riptide"
    );
    public static readonly Enchantment Density = new(
        "minecraft:density", 5, IsMace, (short)EnchantType.Density, 2, "minecraft:breach"
    );
    public static readonly Enchantment DepthStrider = new(
        "minecraft:depth_strider", 3, static item => item.ArmorSlot == ArmorSlot.Feet, (short)EnchantType.DepthStrider, 2,
        "minecraft:frost_walker"
    );
    public static readonly Enchantment FeatherFalling = new(
        "minecraft:feather_falling", 4, static item => item.ArmorSlot == ArmorSlot.Feet, (short)EnchantType.FeatherFalling, 1
    );
    public static readonly Enchantment FireAspect = new(
        "minecraft:fire_aspect", 2, IsSword, (short)EnchantType.FireAspect, 2
    );
    public static readonly Enchantment FireProtection = new(
        "minecraft:fire_protection", 4, static item => item.ArmorSlot is not null, (short)EnchantType.FireProtection, 2,
        "minecraft:protection", "minecraft:blast_protection", "minecraft:projectile_protection"
    );
    public static readonly Enchantment Flame = new(
        "minecraft:flame", 1, IsBow, (short)EnchantType.Flame, 4
    );
    public static readonly Enchantment FrostWalker = new(
        "minecraft:frost_walker", 2, static item => item.ArmorSlot == ArmorSlot.Feet, (short)EnchantType.FrostWalker, 4,
        "minecraft:depth_strider"
    );
    public static readonly Enchantment Impaling = new(
        "minecraft:impaling", 5, IsTrident, (short)EnchantType.Impaling, 2
    );
    public static readonly Enchantment Infinity = new(
        "minecraft:infinity", 1, IsBow, (short)EnchantType.Infinity, 4, "minecraft:mending"
    );
    public static readonly Enchantment Knockback = new(
        "minecraft:knockback", 2, IsSword, (short)EnchantType.Knockback, 2
    );
    public static readonly Enchantment Looting = new(
        "minecraft:looting", 3, IsSword, (short)EnchantType.Looting, 2
    );
    public static readonly Enchantment LuckOfTheSea = new(
        "minecraft:luck_of_the_sea", 3, IsFishingRod, (short)EnchantType.LuckOfTheSea, 2
    );
    public static readonly Enchantment Lunge = new(
        "minecraft:lunge", 3, IsSpear, (short)EnchantType.Lunge, 2
    );
    public static readonly Enchantment Lure = new(
        "minecraft:lure", 3, IsFishingRod, (short)EnchantType.Lure, 2
    );
    public static readonly Enchantment Mending = new(
        "minecraft:mending", 1, static item => item.MaxDurability > 0, (short)EnchantType.Mending, 4,
        "minecraft:infinity"
    );
    public static readonly Enchantment Multishot = new(
        "minecraft:multishot", 1, IsCrossbow, (short)EnchantType.Multishot, 4, "minecraft:piercing"
    );
    public static readonly Enchantment Piercing = new(
        "minecraft:piercing", 4, IsCrossbow, (short)EnchantType.Piercing, 1, "minecraft:multishot"
    );
    public static readonly Enchantment ProjectileProtection = new(
        "minecraft:projectile_protection", 4, static item => item.ArmorSlot is not null, (short)EnchantType.ProjectileProtection, 1,
        "minecraft:protection", "minecraft:fire_protection", "minecraft:blast_protection"
    );
    public static readonly Enchantment Power = new(
        "minecraft:power", 5, IsBow, (short)EnchantType.Power, 1
    );
    public static readonly Enchantment Punch = new(
        "minecraft:punch", 2, IsBow, (short)EnchantType.Punch, 2
    );
    public static readonly Enchantment QuickCharge = new(
        "minecraft:quick_charge", 3, IsCrossbow, (short)EnchantType.QuickCharge, 2
    );
    public static readonly Enchantment Respiration = new(
        "minecraft:respiration", 3, static item => item.ArmorSlot == ArmorSlot.Head, (short)EnchantType.Respiration, 2
    );
    public static readonly Enchantment Riptide = new(
        "minecraft:riptide", 3, IsTrident, (short)EnchantType.Riptide, 4,
        "minecraft:loyalty", "minecraft:channeling"
    );
    public static readonly Enchantment Sharpness = new(
        "minecraft:sharpness", 5, IsMeleeWeapon, (short)EnchantType.Sharpness, 1,
        "minecraft:smite", "minecraft:bane_of_arthropods"
    );
    public static readonly Enchantment Smite = new(
        "minecraft:smite", 5, IsMeleeWeapon, (short)EnchantType.Smite, 2,
        "minecraft:sharpness", "minecraft:bane_of_arthropods"
    );
    public static readonly Enchantment SoulSpeed = new(
        "minecraft:soul_speed", 3, static item => item.ArmorSlot == ArmorSlot.Feet, (short)EnchantType.SoulSpeed, 4
    );
    public static readonly Enchantment SwiftSneak = new(
        "minecraft:swift_sneak", 3, static item => item.ArmorSlot == ArmorSlot.Legs, (short)EnchantType.SwiftSneak, 4
    );
    public static readonly Enchantment Thorns = new(
        "minecraft:thorns", 3, static item => item.ArmorSlot is not null, (short)EnchantType.Thorns, 4
    );
    public static readonly Enchantment Vanishing = new(
        "minecraft:vanishing", 1, IsEnchantable, (short)EnchantType.CurseOfVanishing, 8
    );
    public static readonly Enchantment WindBurst = new(
        "minecraft:wind_burst", 3, IsMace, (short)EnchantType.WindBurst, 4
    );
    public static readonly Enchantment Loyalty = new(
        "minecraft:loyalty", 3, IsTrident, (short)EnchantType.Loyalty, 1, "minecraft:riptide"
    );

    private static readonly Enchantment[] enchantments = [
        AquaAffinity, BaneOfArthropods, Binding, BlastProtection, Breach, Channeling, Density, DepthStrider,
        Efficiency, FeatherFalling, FireAspect, FireProtection, Flame, Fortune, FrostWalker, Impaling, Knockback,
        Looting, Loyalty, LuckOfTheSea, Lunge, Lure, Mending, Multishot, Piercing, Power, ProjectileProtection,
        Protection, Punch, QuickCharge, Respiration, Riptide, Sharpness, SilkTouch, Smite, SoulSpeed, SwiftSneak,
        Thorns, Unbreaking, Vanishing, WindBurst, Infinity
    ];
    private static readonly Dictionary<short, Enchantment> ByBedrockId = enchantments.ToDictionary(
        static enchantment => enchantment.BedrockId!.Value
    );

    public static ReadOnlySpan<Enchantment> Enchantments => enchantments;

    public static Enchantment? Find(string identifier) {
        foreach (Enchantment enchantment in enchantments)
            if (string.Equals(enchantment.Identifier, identifier, StringComparison.Ordinal))
                return enchantment;
        return null;
    }

    public static Enchantment? Find(short bedrockId) => ByBedrockId.GetValueOrDefault(bedrockId);

    private static bool IsSword(ItemType item) => item.ToolType.HasFlag(ToolType.Sword);
    private static bool IsMeleeWeapon(ItemType item) => IsSword(item) || item.Identifier.EndsWith("_axe", StringComparison.Ordinal);
    private static bool IsMace(ItemType item) => item.Identifier == "minecraft:mace";
    private static bool IsBow(ItemType item) => item.Identifier == "minecraft:bow";
    private static bool IsCrossbow(ItemType item) => item.Identifier == "minecraft:crossbow";
    private static bool IsFishingRod(ItemType item) => item.Identifier == "minecraft:fishing_rod";
    private static bool IsTrident(ItemType item) => item.Identifier == "minecraft:trident";
    private static bool IsSpear(ItemType item) => item.Identifier.Contains("spear", StringComparison.Ordinal);
    private static bool IsEnchantable(ItemType item) =>
        item.PropertiesPayload.Components.Contains("minecraft:enchantable", StringComparer.Ordinal);
}
