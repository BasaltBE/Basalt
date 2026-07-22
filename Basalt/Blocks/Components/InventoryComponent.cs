using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class InventoryComponent : BlockComponent {
    public static new string Identifier => "minecraft:inventory";
    public override string ComponentIdentifier => "minecraft:inventory";

    public int ContainerSize { get; }

    public InventoryComponent(int containerSize = 0) {
        ContainerSize = containerSize;
    }

    public static InventoryComponent FromJson(JsonElement element) {
        int containerSize = 0;
        if (element.TryGetProperty("container", out JsonElement container) && container.ValueKind == JsonValueKind.Object) {
            if (container.TryGetProperty("size", out JsonElement size) && size.ValueKind == JsonValueKind.Number) {
                containerSize = size.GetInt32();
            }
        }

        return new InventoryComponent(containerSize);
    }
}
