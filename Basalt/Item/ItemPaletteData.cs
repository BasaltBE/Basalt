using System.Text.Json.Serialization;

namespace Basalt.Item;

public sealed class ItemTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("maxAmount")]
    public int MaxAmount { get; set; } = 64;
}

public sealed class ItemMetadataData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("networkId")]
    public int NetworkId { get; set; }

    [JsonPropertyName("isComponentBased")]
    public bool IsComponentBased { get; set; }

    [JsonPropertyName("itemVersion")]
    public int ItemVersion { get; set; } = 1;

    [JsonPropertyName("properties")]
    public string Properties { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<ItemTypeData>))]
[JsonSerializable(typeof(List<ItemMetadataData>))]
internal partial class ItemPaletteJsonContext : JsonSerializerContext
{
}
