namespace Basalt.Core.Entity;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class EntityTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = [];

    [JsonPropertyName("minecraft:loot")]
    public EntityLootData? Loot { get; set; }

    [JsonPropertyName("propertiesPayload")]
    public EntityPropertiesPayloadData? PropertiesPayload { get; set; }
}

public sealed class EntityLootData
{
    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;
}

public sealed class EntityPropertiesPayloadData
{
    [JsonPropertyName("components")]
    public Dictionary<string, JsonElement> Components { get; set; } = [];

    [JsonPropertyName("componentGroups")]
    public Dictionary<string, Dictionary<string, JsonElement>> ComponentGroups { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<EntityTypeData>))]
internal partial class EntityPaletteJsonContext : JsonSerializerContext
{
}






