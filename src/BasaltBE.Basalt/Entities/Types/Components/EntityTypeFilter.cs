using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityTypeFilter {
    [JsonInclude, JsonPropertyName("filters")]
    public EntityFilter? Filters;

    [JsonInclude, JsonPropertyName("max_dist")]
    public double? MaxDistance;

    [JsonInclude, JsonPropertyName("must_see")]
    public bool? MustSee;

    [JsonInclude, JsonPropertyName("sprint_speed_multiplier")]
    public double? SprintSpeedMultiplier;

    [JsonInclude, JsonPropertyName("walk_speed_multiplier")]
    public double? WalkSpeedMultiplier;
}
