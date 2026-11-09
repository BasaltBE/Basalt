using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class InventoryComponent {
    [JsonInclude, JsonPropertyName("container")]
    public InventoryContainer Container = new();
}
