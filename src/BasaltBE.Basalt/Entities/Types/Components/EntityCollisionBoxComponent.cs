using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityCollisionBoxComponent {
    [JsonInclude, JsonPropertyName("height")]
    public double? Height;

    [JsonInclude, JsonPropertyName("width")]
    public double? Width;
}
