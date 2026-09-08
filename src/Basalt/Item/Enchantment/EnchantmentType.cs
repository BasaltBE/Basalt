namespace Basalt.Core.Item.Enchantment;

using Basalt.Core.Item.Components;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Registered enchantment definition loaded from protocol data.
/// </summary>
public class EnchantmentType {
    private static readonly Dictionary<int, EnchantmentType> ById = [];
    private static readonly Dictionary<string, EnchantmentType> ByIdentifier = new(StringComparer.Ordinal);
    private static bool _loaded;
    private static readonly object LoadLock = new();

    public string Identifier { get; }
    public int Id { get; }
    public int MaxLevel { get; }

    public EnchantmentType(string identifier, int id, int maxLevel) {
        Identifier = identifier;
        Id = id;
        MaxLevel = maxLevel;
    }

    public bool CanApplyTo(ItemStack item) {
        return Identifier switch {
            "protection" or "fire_protection" or "blast_protection" or "projectile_protection" or "thorns" or
                "binding" => HasTag(item, "minecraft:is_armor"),
            "feather_falling" or "depth_strider" or "frost_walker" or "soul_speed" =>
                IsWearableSlot(item, "boots"),
            "respiration" or "aqua_affinity" => IsWearableSlot(item, "helmet"),
            "swift_sneak" => IsWearableSlot(item, "leggings"),
            "sharpness" or "smite" or "bane_of_arthropods" or "fire_aspect" =>
                HasTag(item, "minecraft:is_sword") || HasTag(item, "minecraft:is_axe"),
            "knockback" or "looting" => HasTag(item, "minecraft:is_sword"),
            "efficiency" or "silk_touch" or "fortune" =>
                HasToolTag(item),
            "power" or "punch" or "flame" or "infinity" =>
                item.Identifier == "minecraft:bow",
            "luck_of_the_sea" or "lure" =>
                item.Identifier == "minecraft:fishing_rod",
            "impaling" or "riptide" or "loyalty" or "channeling" =>
                item.Identifier == "minecraft:trident",
            "multishot" or "piercing" or "quick_charge" =>
                item.Identifier == "minecraft:crossbow",
            "density" or "breach" or "wind_burst" or "lunge" =>
                item.Identifier == "minecraft:mace",
            "unbreaking" or "mending" or "vanishing" =>
                item.Type.Components.GetComponent<ItemTypeDurabilityComponent>() is not null,
            _ => true
        };
    }

    public bool ConflictsWith(EnchantmentType other) {
        return (Identifier, other.Identifier) switch {
            ("protection", "fire_protection" or "blast_protection" or "projectile_protection") => true,
            ("fire_protection", "protection" or "blast_protection" or "projectile_protection") => true,
            ("blast_protection", "protection" or "fire_protection" or "projectile_protection") => true,
            ("projectile_protection", "protection" or "fire_protection" or "blast_protection") => true,
            ("sharpness", "smite" or "bane_of_arthropods") => true,
            ("smite", "sharpness" or "bane_of_arthropods") => true,
            ("bane_of_arthropods", "sharpness" or "smite") => true,
            ("silk_touch", "fortune") or ("fortune", "silk_touch") => true,
            ("depth_strider", "frost_walker") or ("frost_walker", "depth_strider") => true,
            ("mending", "infinity") or ("infinity", "mending") => true,
            ("loyalty", "riptide") or ("riptide", "loyalty") => true,
            ("multishot", "piercing") or ("piercing", "multishot") => true,
            ("density", "breach") or ("breach", "density") => true,
            _ => false
        };
    }

    public static IReadOnlyDictionary<int, EnchantmentType> All => ById;

    public static EnchantmentType? Get(int id) {
        return ById.TryGetValue(id, out EnchantmentType? type) ? type : null;
    }

    public static EnchantmentType? Get(string identifier) {
        return ByIdentifier.TryGetValue(identifier, out EnchantmentType? type) ? type : null;
    }

    /// <summary>
    /// Registers a custom enchantment type
    /// </summary>
    public static void Register(EnchantmentType type) {
        ById[type.Id] = type;
        ByIdentifier[type.Identifier] = type;
    }

    /// <summary>
    /// Bonus attack damage for the given level. Override in subclasses.
    /// </summary>
    public virtual float GetAttackBonus(int level) => 0f;

    /// <summary>
    /// Bonus protection for the given level. Override in subclasses.
    /// </summary>
    public virtual float GetProtectionBonus(int level) => 0f;

    /// <summary>
    /// Bonus mining speed for the given level. Override in subclasses.
    /// </summary>
    public virtual float GetMiningSpeedBonus(int level) => 0f;

    /// <summary>
    /// Called when the holder breaks a block.
    /// </summary>
    public virtual void OnBlockBreak(int level, BlockBreakEnchantmentContext ctx) { }

    /// <summary>
    /// Called when the holder attacks an entity.
    /// </summary>
    public virtual void OnAttackEntity(int level, AttackEntityEnchantmentContext ctx) { }

    /// <summary>
    /// Called when the wearer takes damage.
    /// </summary>
    public virtual void OnHurt(int level, HurtEnchantmentContext ctx) { }

    /// <summary>
    /// Called every tick while the item is equipped.
    /// </summary>
    public virtual void OnTick(int level, TickEnchantmentContext ctx) { }

    private static bool HasTag(ItemStack item, string tag) {
        return item.Type.Tags.Contains(tag, StringComparer.Ordinal);
    }

    private static bool HasToolTag(ItemStack item) {
        return HasTag(item, "minecraft:is_sword") ||
            HasTag(item, "minecraft:is_axe") ||
            HasTag(item, "minecraft:is_pickaxe") ||
            HasTag(item, "minecraft:is_shovel") ||
            HasTag(item, "minecraft:is_hoe");
    }

    private static bool IsWearableSlot(ItemStack item, string slot) {
        return HasTag(item, "minecraft:is_armor") && item.Identifier.EndsWith($"_{slot}", StringComparison.Ordinal);
    }

    public static void Load(string? dataDirectory = null) {
        if (_loaded) return;

        lock (LoadLock) {
            if (_loaded) return;

            EnchantmentRegistry.RegisterVanilla();

            List<EnchantmentTypeData> entries;
            if (!string.IsNullOrWhiteSpace(dataDirectory)) {
                string path = Path.Combine(dataDirectory, "enchantment_types.json");
                using FileStream fileStream = File.OpenRead(path);
                entries = JsonSerializer.Deserialize(fileStream, EnchantmentJsonContext.Default.ListEnchantmentTypeData) ?? [];
            }
            else {
                using Stream stream = ProtocolData.Require("enchantment_types.json");
                entries = JsonSerializer.Deserialize(stream, EnchantmentJsonContext.Default.ListEnchantmentTypeData) ?? [];
            }

            ById.EnsureCapacity(entries.Count);
            ByIdentifier.EnsureCapacity(entries.Count);

            for (int i = 0; i < entries.Count; i++) {
                EnchantmentTypeData entry = entries[i];
                if (string.IsNullOrEmpty(entry.Identifier)) continue;

                if (ById.ContainsKey(entry.Id)) continue;

                EnchantmentType enchantment = new(entry.Identifier, entry.Id, entry.MaxLevel);
                ById[enchantment.Id] = enchantment;
                ByIdentifier[enchantment.Identifier] = enchantment;
            }

            _loaded = true;
        }
    }

}

internal sealed class EnchantmentTypeData {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<EnchantmentTypeData>))]
internal sealed partial class EnchantmentJsonContext : JsonSerializerContext {
}
