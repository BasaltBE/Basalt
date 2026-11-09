using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class RedstoneProducerComponent {
    [JsonInclude, JsonPropertyName("power")]
    public int Power;
}
