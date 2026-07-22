using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class FluidContainerComponent : BlockComponent {
    public static new string Identifier => "minecraft:fluid_container";
    public override string ComponentIdentifier => "minecraft:fluid_container";

    public int FillLevel { get; }
    public ColorData FluidColor { get; }

    public FluidContainerComponent(int fillLevel = 0, ColorData? fluidColor = null) {
        FillLevel = fillLevel;
        FluidColor = fluidColor ?? new ColorData();
    }

    public static FluidContainerComponent FromJson(JsonElement element) {
        int fillLevel = 0;
        ColorData fluidColor = new();

        if (element.TryGetProperty("fillLevel", out JsonElement fillEl) && fillEl.ValueKind == JsonValueKind.Number) {
            fillLevel = fillEl.GetInt32();
        }

        if (element.TryGetProperty("fluidColor", out JsonElement colorEl) && colorEl.ValueKind == JsonValueKind.Object) {
            fluidColor = ColorData.FromJson(colorEl);
        }

        return new FluidContainerComponent(fillLevel, fluidColor);
    }
}
