namespace Basalt.Core.Item;

using System.Text.Json;
using System.Text.Json.Serialization;


public sealed class ItemTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("maxAmount")]
    public int MaxAmount { get; set; } = 64;

    [JsonPropertyName("componentBased")]
    public bool ComponentBased { get; set; }

    [JsonPropertyName("networkId")]
    public int? NetworkId { get; set; }

    [JsonPropertyName("itemVersion")]
    public int ItemVersion { get; set; } = 1;

    [JsonPropertyName("propertiesPayload")]
    public JsonElement? PropertiesPayload { get; set; }

    [JsonPropertyName("catalog")]
    public ItemCatalogData? Catalog { get; set; }
}

public sealed class ItemCatalogData
{
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("group_identifier")]
    public ItemGroupIdentifierData? GroupIdentifier { get; set; }
}

public sealed class ItemGroupIdentifierData
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<ItemTypeData>))]
[JsonSerializable(typeof(CreativeContentJson))]
internal sealed partial class ItemPaletteJsonContext : JsonSerializerContext
{
}

public sealed class CreativeContentJson
{
    [JsonPropertyName("Groups")]
    public List<CreativeGroupJson> Groups { get; set; } = [];

    [JsonPropertyName("Items")]
    public List<CreativeItemJson> Items { get; set; } = [];
}

public sealed class CreativeGroupJson
{
    [JsonPropertyName("Category")]
    public int Category { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Icon")]
    public CreativeItemStackJson Icon { get; set; } = new();
}

public sealed class CreativeItemJson
{
    [JsonPropertyName("CreativeItemNetworkID")]
    public int CreativeItemNetworkID { get; set; }

    [JsonPropertyName("Item")]
    public CreativeItemStackJson Item { get; set; } = new();

    [JsonPropertyName("GroupIndex")]
    public int GroupIndex { get; set; }
}

public sealed class CreativeItemStackJson
{
    [JsonPropertyName("NetworkID")]
    public int NetworkID { get; set; }

    [JsonPropertyName("MetadataValue")]
    public uint MetadataValue { get; set; }

    [JsonPropertyName("BlockRuntimeID")]
    public int BlockRuntimeID { get; set; }

    [JsonPropertyName("Count")]
    public int Count { get; set; } = 1;
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(CreativeContentJson))]
internal sealed partial class CreativeContentJsonContext : JsonSerializerContext
{
}






