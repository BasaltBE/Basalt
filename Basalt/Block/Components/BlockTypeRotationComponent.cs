namespace Basalt.Server.Block.Components;

using Basalt.Server.Block.Traits.Types;


public sealed class BlockTypeRotationComponent : BlockTypeComponent
{
    public new static string Identifier => "minecraft:rotation";

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







