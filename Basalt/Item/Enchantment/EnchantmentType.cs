namespace Basalt.Core.Item.Enchantment;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Registered enchantment definition loaded from protocol data.
/// </summary>
public class EnchantmentType
{
    private static readonly Dictionary<int, EnchantmentType> ById = [];
    private static readonly Dictionary<string, EnchantmentType> ByIdentifier = new(StringComparer.Ordinal);
    private static bool _loaded;
    private static readonly object LoadLock = new();

    public string Identifier { get; }
    public int Id { get; }
    public int MaxLevel { get; }

    public EnchantmentType(string identifier, int id, int maxLevel)
    {
        Identifier = identifier;
        Id = id;
        MaxLevel = maxLevel;
    }

    public static IReadOnlyDictionary<int, EnchantmentType> All => ById;

    public static EnchantmentType? Get(int id)
    {
        return ById.TryGetValue(id, out EnchantmentType? type) ? type : null;
    }

    public static EnchantmentType? Get(string identifier)
    {
        return ByIdentifier.TryGetValue(identifier, out EnchantmentType? type) ? type : null;
    }

    /// <summary>
    /// Registers a custom enchantment type
    /// </summary>
    public static void Register(EnchantmentType type)
    {
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

    public static void Load(string? dataDirectory = null)
    {
        if (_loaded) return;

        lock (LoadLock)
        {
            if (_loaded) return;

            EnchantmentRegistry.RegisterVanilla();

            List<EnchantmentTypeData> entries;
            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                string path = Path.Combine(dataDirectory, "enchantment_types.json");
                using FileStream fileStream = File.OpenRead(path);
                entries = JsonSerializer.Deserialize(fileStream, EnchantmentJsonContext.Default.ListEnchantmentTypeData) ?? [];
            }
            else
            {
                using Stream stream = ProtocolData.Require("enchantment_types.json");
                entries = JsonSerializer.Deserialize(stream, EnchantmentJsonContext.Default.ListEnchantmentTypeData) ?? [];
            }

            ById.EnsureCapacity(entries.Count);
            ByIdentifier.EnsureCapacity(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
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

internal sealed class EnchantmentTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<EnchantmentTypeData>))]
internal sealed partial class EnchantmentJsonContext : JsonSerializerContext
{
}
