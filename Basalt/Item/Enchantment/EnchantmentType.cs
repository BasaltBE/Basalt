namespace Basalt.Core.Item.Enchantment;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a registered enchantment definition loaded from protocol data.
/// </summary>
public sealed class EnchantmentType
{
  private static readonly Dictionary<int, EnchantmentType> ById = [];
  private static readonly Dictionary<string, EnchantmentType> ByIdentifier = new(StringComparer.Ordinal);
  private static bool _loaded;
  private static readonly object LoadLock = new();

  public string Identifier { get; }
  public int Id { get; }
  public int MaxLevel { get; }

  private EnchantmentType(string identifier, int id, int maxLevel)
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

  public static void Load(string? dataDirectory = null)
  {
    if (_loaded) return;

    lock (LoadLock)
    {
      if (_loaded) return;

      string root = ResolveDataRoot(dataDirectory);
      string path = Path.Combine(root, "enchantment_types.json");

      List<EnchantmentTypeData> entries;
      using (FileStream stream = File.OpenRead(path))
      {
        entries = JsonSerializer.Deserialize(stream, EnchantmentJsonContext.Default.ListEnchantmentTypeData) ?? [];
      }

      ById.EnsureCapacity(entries.Count);
      ByIdentifier.EnsureCapacity(entries.Count);

      for (int i = 0; i < entries.Count; i++)
      {
        EnchantmentTypeData entry = entries[i];
        if (string.IsNullOrEmpty(entry.Identifier))
        {
          continue;
        }

        EnchantmentType enchantment = new(entry.Identifier, entry.Id, entry.MaxLevel);
        ById[enchantment.Id] = enchantment;
        ByIdentifier[enchantment.Identifier] = enchantment;
      }

      _loaded = true;
    }
  }

  private static string ResolveDataRoot(string? dataDirectory = null)
  {
    if (!string.IsNullOrWhiteSpace(dataDirectory))
    {
      return dataDirectory;
    }

    string? current = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(current))
    {
      string candidate = Path.Combine(current, "Protocol", "Data");
      if (Directory.Exists(candidate))
      {
        return candidate;
      }

      DirectoryInfo? parent = Directory.GetParent(current);
      if (parent is null)
      {
        break;
      }

      current = parent.FullName;
    }

    throw new DirectoryNotFoundException("Could not locate Protocol/Data directory.");
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
internal partial class EnchantmentJsonContext : JsonSerializerContext
{
}
