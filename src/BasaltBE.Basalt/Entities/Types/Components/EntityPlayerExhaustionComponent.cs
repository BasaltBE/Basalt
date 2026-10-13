using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityPlayerExhaustionComponent {
    [JsonInclude, JsonPropertyName("max")]
    public double? Max;

    [JsonInclude, JsonPropertyName("value")]
    public double? Value;
}
