namespace Basalt.Core.Blocks.Components;

using System.Text.Json;
using Basalt.Core.Blocks.Traits.Types;

public sealed class MovableComponent : BlockComponent
{
    public static new string Identifier => "minecraft:movable";
    public override string ComponentIdentifier => "minecraft:movable";

    public MovementType Movement { get; }
    public StickyType Sticky { get; }

    public bool CanBePushed => Movement is MovementType.PushPull or MovementType.PushOnly;
    public bool CanBePulled => Movement == MovementType.PushPull;
    public bool IsSticky => Sticky != StickyType.None;

    public MovableComponent(MovementType movement = MovementType.PushPull, StickyType sticky = StickyType.None)
    {
        Movement = movement;
        Sticky = sticky;
    }

    public static MovableComponent FromJson(JsonElement element)
    {
        MovementType movement = MovementType.PushPull;
        StickyType sticky = StickyType.None;

        if (element.TryGetProperty("movementType", out JsonElement movementEl) && movementEl.ValueKind == JsonValueKind.String)
        {
            string? value = movementEl.GetString();
            if (value is not null && Enum.TryParse(value, out MovementType parsed))
            {
                movement = parsed;
            }
        }

        if (element.TryGetProperty("stickyType", out JsonElement stickyEl) && stickyEl.ValueKind == JsonValueKind.String)
        {
            string? value = stickyEl.GetString();
            if (value is not null && Enum.TryParse(value, out StickyType parsed))
            {
                sticky = parsed;
            }
        }

        return new MovableComponent(movement, sticky);
    }
}

public sealed class PrecipitationInteractionsComponent : BlockComponent
{
    public static new string Identifier => "minecraft:precipitation_interactions";
    public override string ComponentIdentifier => "minecraft:precipitation_interactions";

    public PrecipitationInteractionsComponent()
    {
    }
}

public sealed class RedstoneProducerComponent : BlockComponent
{
    public static new string Identifier => "minecraft:redstone_producer";
    public override string ComponentIdentifier => "minecraft:redstone_producer";

    public int Power { get; }

    public RedstoneProducerComponent(int power = 0)
    {
        Power = power;
    }

    public static RedstoneProducerComponent FromJson(JsonElement element)
    {
        int power = 0;
        if (element.TryGetProperty("power", out JsonElement powerEl) && powerEl.ValueKind == JsonValueKind.Number)
        {
            power = powerEl.GetInt32();
        }

        return new RedstoneProducerComponent(power);
    }
}

public sealed class SignComponent : BlockComponent
{
    public static new string Identifier => "minecraft:sign";
    public override string ComponentIdentifier => "minecraft:sign";

    public bool IsWaxed { get; }

    public SignComponent(bool isWaxed = false)
    {
        IsWaxed = isWaxed;
    }

    public static SignComponent FromJson(JsonElement element)
    {
        bool isWaxed = false;
        if (element.TryGetProperty("isWaxed", out JsonElement waxedEl))
        {
            isWaxed = waxedEl.ValueKind == JsonValueKind.True;
        }

        return new SignComponent(isWaxed);
    }
}

public sealed class InventoryComponent : BlockComponent
{
    public static new string Identifier => "minecraft:inventory";
    public override string ComponentIdentifier => "minecraft:inventory";

    public int ContainerSize { get; }

    public InventoryComponent(int containerSize = 0)
    {
        ContainerSize = containerSize;
    }

    public static InventoryComponent FromJson(JsonElement element)
    {
        int containerSize = 0;
        if (element.TryGetProperty("container", out JsonElement container) && container.ValueKind == JsonValueKind.Object)
        {
            if (container.TryGetProperty("size", out JsonElement size) && size.ValueKind == JsonValueKind.Number)
            {
                containerSize = size.GetInt32();
            }
        }

        return new InventoryComponent(containerSize);
    }
}

public sealed class PistonComponent : BlockComponent
{
    public static new string Identifier => "minecraft:piston";
    public override string ComponentIdentifier => "minecraft:piston";

    public bool IsMoving { get; }
    public string State { get; }

    public PistonComponent(bool isMoving = false, string state = "Retracted")
    {
        IsMoving = isMoving;
        State = state;
    }

    public static PistonComponent FromJson(JsonElement element)
    {
        bool isMoving = false;
        string state = "Retracted";

        if (element.TryGetProperty("isMoving", out JsonElement movingEl))
        {
            isMoving = movingEl.ValueKind == JsonValueKind.True;
        }

        if (element.TryGetProperty("state", out JsonElement stateEl) && stateEl.ValueKind == JsonValueKind.String)
        {
            state = stateEl.GetString() ?? "Retracted";
        }

        return new PistonComponent(isMoving, state);
    }
}

public sealed class FluidContainerComponent : BlockComponent
{
    public static new string Identifier => "minecraft:fluid_container";
    public override string ComponentIdentifier => "minecraft:fluid_container";

    public int FillLevel { get; }
    public ColorData FluidColor { get; }

    public FluidContainerComponent(int fillLevel = 0, ColorData? fluidColor = null)
    {
        FillLevel = fillLevel;
        FluidColor = fluidColor ?? new ColorData();
    }

    public static FluidContainerComponent FromJson(JsonElement element)
    {
        int fillLevel = 0;
        ColorData fluidColor = new();

        if (element.TryGetProperty("fillLevel", out JsonElement fillEl) && fillEl.ValueKind == JsonValueKind.Number)
        {
            fillLevel = fillEl.GetInt32();
        }

        if (element.TryGetProperty("fluidColor", out JsonElement colorEl) && colorEl.ValueKind == JsonValueKind.Object)
        {
            fluidColor = ColorData.FromJson(colorEl);
        }

        return new FluidContainerComponent(fillLevel, fluidColor);
    }
}

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

public sealed class RecordPlayerComponent : BlockComponent
{
    public static new string Identifier => "minecraft:record_player";
    public override string ComponentIdentifier => "minecraft:record_player";

    public RecordPlayerComponent()
    {
    }
}

public sealed class RotationComponent : BlockComponent
{
    public static new string Identifier => "minecraft:rotation";
    public override string ComponentIdentifier => "minecraft:rotation";

    public RotationComponent()
    {
    }

    public static CardinalDirection GetCardinalDirection(float yaw)
    {
        float normalized = NormalizeYaw(yaw);

        if (normalized >= 45f && normalized < 135f)
        {
            return CardinalDirection.West;
        }

        if (normalized >= 135f && normalized < 225f)
        {
            return CardinalDirection.North;
        }

        if (normalized >= 225f && normalized < 315f)
        {
            return CardinalDirection.East;
        }

        return CardinalDirection.South;
    }

    public static float NormalizeYaw(float yaw)
    {
        float normalized = yaw % 360f;
        if (normalized < 0f)
        {
            normalized += 360f;
        }

        return normalized;
    }
}

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

public enum MovementType
{
    PushPull,
    PushOnly,
    None
}

public enum StickyType
{
    None,
    Side,
    All
}
