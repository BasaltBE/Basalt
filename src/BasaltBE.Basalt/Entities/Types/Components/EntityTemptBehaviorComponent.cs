using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityTemptBehaviorComponent {
    [JsonInclude, JsonPropertyName("can_get_scared")]
    public bool? CanGetScared;

    [JsonInclude, JsonPropertyName("items")]
    public string[]? Items;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("speed_multiplier")]
    public double? SpeedMultiplier;

    [JsonInclude, JsonPropertyName("within_radius")]
    public double? WithinRadius;
}
