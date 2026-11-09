using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityNearestAttackableTargetBehaviorComponent {
    [JsonInclude, JsonPropertyName("attack_interval")]
    public EntityNumericValue? AttackInterval;

    [JsonInclude, JsonPropertyName("entity_types")]
    public EntityTypeFilter[]? EntityTypes;

    [JsonInclude, JsonPropertyName("must_reach")]
    public bool? MustReach;

    [JsonInclude, JsonPropertyName("must_see")]
    public bool? MustSee;

    [JsonInclude, JsonPropertyName("must_see_forget_duration")]
    public double? MustSeeForgetDuration;

    [JsonInclude, JsonPropertyName("persist_time")]
    public double? PersistTime;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("reselect_targets")]
    public bool? ReselectTargets;

    [JsonInclude, JsonPropertyName("scan_interval")]
    public int? ScanInterval;

    [JsonInclude, JsonPropertyName("set_persistent")]
    public bool? SetPersistent;

    [JsonInclude, JsonPropertyName("target_search_height")]
    public double? TargetSearchHeight;

    [JsonInclude, JsonPropertyName("within_radius")]
    public double? WithinRadius;
}
