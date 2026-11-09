using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityMovementJumpComponent {
    [JsonInclude, JsonPropertyName("jump_delay")]
    public double[]? JumpDelay;
}
