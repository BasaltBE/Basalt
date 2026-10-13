using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityPlayerSaturationComponent {
    [JsonInclude, JsonPropertyName("value")]
    public double? Value;
}
