using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityAvoidMobTypeBehaviorComponent {
    [JsonInclude, JsonPropertyName("entity_types")]
    public EntityTypeFilter[]? EntityTypes;

    [JsonInclude, JsonPropertyName("ignore_visibility")]
    public bool? IgnoreVisibility;

    [JsonInclude, JsonPropertyName("max_dist")]
    public double? MaxDistance;

    [JsonInclude, JsonPropertyName("max_flee")]
    public double? MaxFlee;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("probability_per_strength")]
    public double? ProbabilityPerStrength;
}
