using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityBreathableComponent {
    [JsonInclude, JsonPropertyName("breathes_air")]
    public bool? BreathesAir;

    [JsonInclude, JsonPropertyName("breathes_lava")]
    public bool? BreathesLava;

    [JsonInclude, JsonPropertyName("breathes_water")]
    public bool? BreathesWater;

    [JsonInclude, JsonPropertyName("generates_bubbles")]
    public bool? GeneratesBubbles;

    [JsonInclude, JsonPropertyName("inhale_time")]
    public double? InhaleTime;

    [JsonInclude, JsonPropertyName("suffocateTime")]
    public double? SuffocateTime;

    [JsonInclude, JsonPropertyName("suffocate_time")]
    public double? SuffocateTimeSnakeCase;

    [JsonInclude, JsonPropertyName("totalSupply")]
    public double? TotalSupply;

    [JsonInclude, JsonPropertyName("total_supply")]
    public double? TotalSupplySnakeCase;
}
