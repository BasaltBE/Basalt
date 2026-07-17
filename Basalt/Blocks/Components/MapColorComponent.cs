using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class MapColorComponent : BlockComponent
{
    public static new string Identifier => "minecraft:map_color";
    public override string ComponentIdentifier => "minecraft:map_color";

    public ColorData Color { get; }
    public string? TintMethod { get; }
    public ColorData? TintedColor { get; }

    public MapColorComponent(ColorData? color = null, string? tintMethod = null, ColorData? tintedColor = null)
    {
        Color = color ?? new ColorData();
        TintMethod = tintMethod;
        TintedColor = tintedColor;
    }

    public static MapColorComponent FromJson(JsonElement element)
    {
        ColorData color = new();
        string? tintMethod = null;
        ColorData? tintedColor = null;

        if (element.TryGetProperty("color", out JsonElement colorEl) && colorEl.ValueKind == JsonValueKind.Object)
        {
            color = ColorData.FromJson(colorEl);
        }

        if (element.TryGetProperty("tintMethod", out JsonElement tintEl) && tintEl.ValueKind == JsonValueKind.String)
        {
            tintMethod = tintEl.GetString();
        }

        if (element.TryGetProperty("tintedColor", out JsonElement tintedEl) && tintedEl.ValueKind == JsonValueKind.Object)
        {
            tintedColor = ColorData.FromJson(tintedEl);
        }

        return new MapColorComponent(color, tintMethod, tintedColor);
    }
}
