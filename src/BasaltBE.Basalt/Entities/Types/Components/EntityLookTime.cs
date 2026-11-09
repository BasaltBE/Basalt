using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityLookTime {
    [JsonInclude, JsonPropertyName("min")]
    public double Min;

    [JsonInclude, JsonPropertyName("max")]
    public double Max;
}
