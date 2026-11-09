using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityAttackComponent {
    [JsonInclude, JsonPropertyName("damage")]
    public EntityNumericValue? Damage;

    [JsonInclude, JsonPropertyName("effect_name")]
    public string? EffectName;

    [JsonInclude, JsonPropertyName("effect_duration")]
    public double? EffectDuration;
}
