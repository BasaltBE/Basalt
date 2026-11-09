using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityPanicBehaviorComponent {
    [JsonInclude, JsonPropertyName("damage_sources")]
    public string[]? DamageSources;

    [JsonInclude, JsonPropertyName("prefer_water")]
    public bool? PreferWater;

    [JsonInclude, JsonPropertyName("priority")]
    public int? Priority;

    [JsonInclude, JsonPropertyName("speed_multiplier")]
    public double? SpeedMultiplier;
}
