using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class ColorData
{
    public float Alpha { get; }
    public float Red { get; }
    public float Green { get; }
    public float Blue { get; }

    public ColorData(float alpha = 0, float red = 0, float green = 0, float blue = 0)
    {
        Alpha = alpha;
        Red = red;
        Green = green;
        Blue = blue;
    }

    public static ColorData FromJson(JsonElement element)
    {
        float alpha = 0, red = 0, green = 0, blue = 0;

        if (element.TryGetProperty("alpha", out JsonElement a) && a.ValueKind == JsonValueKind.Number)
        {
            alpha = a.GetSingle();
        }

        if (element.TryGetProperty("red", out JsonElement r) && r.ValueKind == JsonValueKind.Number)
        {
            red = r.GetSingle();
        }

        if (element.TryGetProperty("green", out JsonElement g) && g.ValueKind == JsonValueKind.Number)
        {
            green = g.GetSingle();
        }

        if (element.TryGetProperty("blue", out JsonElement b) && b.ValueKind == JsonValueKind.Number)
        {
            blue = b.GetSingle();
        }

        return new ColorData(alpha, red, green, blue);
    }
}
