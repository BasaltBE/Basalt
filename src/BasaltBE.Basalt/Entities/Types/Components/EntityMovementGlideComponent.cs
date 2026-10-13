using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMovementGlideComponent {
    [JsonInclude, JsonPropertyName("speed_when_turning")]
    public double? SpeedWhenTurning;

    [JsonInclude, JsonPropertyName("start_speed")]
    public double? StartSpeed;
}
