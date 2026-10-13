using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types;

public sealed class EntityTypeProperty {
    [JsonInclude, JsonPropertyName("name")]
    public string Name = string.Empty;

    [JsonInclude, JsonPropertyName("type")]
    public int Type;

    [JsonInclude, JsonPropertyName("values")]
    public string[]? Values;
}
