using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityScaleComponent {
    [JsonInclude, JsonPropertyName("value")]
    public double? Value;
}
