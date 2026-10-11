using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class BlockColor {
    [JsonInclude, JsonPropertyName("alpha")]
    public float Alpha;

    [JsonInclude, JsonPropertyName("green")]
    public float Green;

    [JsonInclude, JsonPropertyName("red")]
    public float Red;

    [JsonInclude, JsonPropertyName("blue")]
    public float Blue;
}
