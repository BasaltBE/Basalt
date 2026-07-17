using System.Text.Json;
using Basalt.Core.Blocks.Traits.Types;

namespace Basalt.Core.Blocks.Components;

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
