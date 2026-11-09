using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityRandomLookAroundBehaviorComponent {
    [JsonInclude, JsonPropertyName("look_distance")]
    public double? LookDistance;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;
}
