using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMovementAmphibiousComponent {
    [JsonInclude, JsonPropertyName("max_turn")]
    public double? MaxTurn;
}
