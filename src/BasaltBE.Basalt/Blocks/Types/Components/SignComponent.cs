using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class SignComponent {
    [JsonInclude, JsonPropertyName("isWaxed")]
    public bool Waxed;
}
