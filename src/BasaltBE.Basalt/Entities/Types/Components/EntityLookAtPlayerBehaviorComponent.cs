using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityLookAtPlayerBehaviorComponent {
    [JsonInclude, JsonPropertyName("angle_of_view_horizontal")]
    public double? AngleOfViewHorizontal;

    [JsonInclude, JsonPropertyName("look_distance")]
    public double? LookDistance;

    [JsonInclude, JsonPropertyName("look_time")]
    public EntityLookTime? LookTime;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("probability")]
    public double? Probability;
}
