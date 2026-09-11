namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

internal static class RedstonePower {
    public static int GetDirectSignal(Dimension dimension, BlockPos target) {
        int signal = 0;
        foreach (BlockPos sourcePosition in Neighbors(target)) {
            BlockPermutation source = dimension.GetLoadedPermutationOrAir(
                sourcePosition.X,
                sourcePosition.Y,
                sourcePosition.Z);
            signal = Math.Max(signal, GetSignal(source, sourcePosition, target));
        }

        return signal;
    }

    public static int GetSignal(BlockPermutation source, BlockPos sourcePosition, BlockPos target) {
        if (source.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
            source.State.TryGetValue("redstone_signal", out BlockStateValue wireSignal) &&
            wireSignal.Kind == 0) {
            return Math.Clamp((int)wireSignal.AsNumber(), 0, 15);
        }

        if (source.Type.GetComponent<RedstoneProducerComponent>() is { Power: > 0 } producer) {
            return Math.Clamp(producer.Power, 0, 15);
        }

        if (source.Type.Identifier == BlockIdentifier.Lever.ToIdentifier() &&
            source.State.TryGetValue("open_bit", out BlockStateValue leverOpen) &&
            leverOpen.Kind == 2 && leverOpen.AsBool()) {
            return 15;
        }

        if (source.Type.Identifier.EndsWith("_button", StringComparison.Ordinal) &&
            source.State.TryGetValue("button_pressed_bit", out BlockStateValue buttonPressed) &&
            buttonPressed.Kind == 2 && buttonPressed.AsBool()) {
            return 15;
        }

        if (source.Type.Identifier.EndsWith("_pressure_plate", StringComparison.Ordinal) &&
            source.State.TryGetValue("redstone_signal", out BlockStateValue plateSignal) &&
            plateSignal.Kind == 0) {
            return Math.Clamp((int)plateSignal.AsNumber(), 0, 15);
        }

        if (source.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier() &&
            source.State.TryGetValue("minecraft:cardinal_direction", out BlockStateValue repeaterDirection) &&
            repeaterDirection.Kind == 1 &&
            PointsTo(ParseFacing(repeaterDirection.AsString()), sourcePosition, target)) {
            return 15;
        }

        if (source.Type.Identifier == BlockIdentifier.Observer.ToIdentifier() &&
            ObserverTrait.Powers(source, sourcePosition, target)) {
            return 15;
        }

        if (source.Type.Identifier == BlockIdentifier.PoweredComparator.ToIdentifier() &&
            source.State.TryGetValue("minecraft:cardinal_direction", out BlockStateValue comparatorDirection) &&
            comparatorDirection.Kind == 1 &&
            PointsTo(ParseFacing(comparatorDirection.AsString()), sourcePosition, target)) {
            return source.State.TryGetValue("output_lit_bit", out BlockStateValue outputLit) &&
                outputLit.Kind == 2 && outputLit.AsBool() ? 15 : 0;
        }

        return 0;
    }

    public static bool PointsTo(FacingDirection direction, BlockPos source, BlockPos target) {
        return direction switch {
            FacingDirection.North => target.X == source.X && target.Z == source.Z - 1,
            FacingDirection.South => target.X == source.X && target.Z == source.Z + 1,
            FacingDirection.East => target.X == source.X + 1 && target.Z == source.Z,
            FacingDirection.West => target.X == source.X - 1 && target.Z == source.Z,
            _ => false
        };
    }

    public static FacingDirection ParseFacing(string direction) => direction switch {
        "down" => FacingDirection.Down,
        "up" => FacingDirection.Up,
        "north" => FacingDirection.North,
        "south" => FacingDirection.South,
        "east" => FacingDirection.East,
        _ => FacingDirection.West
    };

    private static IEnumerable<BlockPos> Neighbors(BlockPos position) {
        yield return new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z };
        yield return new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z };
        yield return new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z };
        yield return new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z };
        yield return new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 };
        yield return new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 };
    }
}
