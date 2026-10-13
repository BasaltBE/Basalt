using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types;

public sealed class EntityType {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("components")]
    public string[] Components = [];

    [JsonInclude, JsonPropertyName("families")]
    public string[] Families = [];

    [JsonInclude, JsonPropertyName("runtimeId")]
    public int? RuntimeId;

    [JsonInclude, JsonPropertyName("summonable")]
    public bool? Summonable;

    [JsonInclude, JsonPropertyName("hasSpawnEgg")]
    public bool? HasSpawnEgg;

    [JsonInclude, JsonPropertyName("blockId")]
    public string? BlockId;

    [JsonInclude, JsonPropertyName("minecraft:loot")]
    public EntityLoot? Loot;

    [JsonInclude, JsonPropertyName("properties")]
    public EntityTypeProperty[] Properties = [];

    [JsonInclude, JsonPropertyName("propertiesPayload")]
    public EntityTypePayload? PropertiesPayload;
}
