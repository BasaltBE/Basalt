using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class InventoryContainer {
    [JsonInclude, JsonPropertyName("size")]
    public int Size;
}
