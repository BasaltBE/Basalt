using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class MovableComponent {
    [JsonInclude, JsonPropertyName("movementType")]
    public string MovementType = string.Empty;

    [JsonInclude, JsonPropertyName("stickyType")]
    public string StickyType = string.Empty;
}
