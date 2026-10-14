using System.Text.Json.Serialization;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemType {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("catalog")]
    public ItemCatalog? Catalog;

    [JsonInclude, JsonPropertyName("tags")]
    public string[] Tags = [];

    [JsonInclude, JsonPropertyName("stackable")]
    public bool Stackable;

    [JsonInclude, JsonPropertyName("maxAmount")]
    public int MaxAmount;

    [JsonInclude, JsonPropertyName("componentBased")]
    public bool ComponentBased;

    [JsonInclude, JsonPropertyName("networkId")]
    public int NetworkId;

    [JsonInclude, JsonPropertyName("itemVersion")]
    public ItemVersion Version;

    [JsonInclude, JsonPropertyName("legacyName")]
    public string? LegacyName;

    [JsonInclude, JsonPropertyName("propertiesPayload")]
    public ItemPropertiesPayload PropertiesPayload = new();
}
