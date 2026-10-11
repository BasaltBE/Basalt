using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class MapColorComponent {
    [JsonInclude, JsonPropertyName("color")]
    public BlockColor Color = new();

    [JsonInclude, JsonPropertyName("tintMethod")]
    public string TintMethod = string.Empty;

    [JsonInclude, JsonPropertyName("tintedColor")]
    public BlockColor TintedColor = new();
}
