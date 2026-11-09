using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMountPathingBehaviorComponent {
    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("speed_multiplier")]
    public double? SpeedMultiplier;

    [JsonInclude, JsonPropertyName("target_dist")]
    public double? TargetDistance;

    [JsonInclude, JsonPropertyName("track_target")]
    public bool? TrackTarget;
}
