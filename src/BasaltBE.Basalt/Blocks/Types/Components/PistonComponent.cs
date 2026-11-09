using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class PistonComponent {
    [JsonInclude, JsonPropertyName("isMoving")]
    public bool Moving;

    [JsonInclude, JsonPropertyName("state")]
    public string State = string.Empty;
}
