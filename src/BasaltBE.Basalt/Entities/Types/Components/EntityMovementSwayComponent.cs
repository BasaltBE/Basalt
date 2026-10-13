using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMovementSwayComponent {
    [JsonInclude, JsonPropertyName("sway_amplitude")]
    public double? SwayAmplitude;
}
