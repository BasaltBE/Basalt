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
}

public sealed class CreativeGroupData
{
    [JsonPropertyName("category")]
    public int Category { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

public sealed class CreativeContentData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("instance")]
    public string Instance { get; set; } = string.Empty;

    [JsonPropertyName("groupIndex")]
    public int GroupIndex { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<ItemTypeData>))]
[JsonSerializable(typeof(List<CreativeGroupData>))]
[JsonSerializable(typeof(List<CreativeContentData>))]
internal partial class ItemPaletteJsonContext : JsonSerializerContext
{
}






