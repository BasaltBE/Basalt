using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMovementBasicComponent {
    [JsonInclude, JsonPropertyName("max_turn")]
    public double? MaxTurn;
}
