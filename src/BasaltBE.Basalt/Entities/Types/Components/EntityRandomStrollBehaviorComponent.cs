using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityRandomStrollBehaviorComponent {
    [JsonInclude, JsonPropertyName("interval")]
    public int? Interval;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("speed_multiplier")]
    public double? SpeedMultiplier;

    [JsonInclude, JsonPropertyName("xz_dist")]
    public double? XzDistance;

    [JsonInclude, JsonPropertyName("y_dist")]
    public double? YDistance;
}
