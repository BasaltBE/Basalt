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
internal partial class ItemPaletteJsonContext : JsonSerializerContext
{
}






