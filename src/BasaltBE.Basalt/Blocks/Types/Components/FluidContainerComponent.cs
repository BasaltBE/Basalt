using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class FluidContainerComponent {
    [JsonInclude, JsonPropertyName("fillLevel")]
    public int FillLevel;

    [JsonInclude, JsonPropertyName("fluidColor")]
    public BlockColor FluidColor = new();
}
