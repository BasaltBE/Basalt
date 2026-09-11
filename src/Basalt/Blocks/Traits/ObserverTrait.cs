namespace Basalt.Core.Blocks.Traits;

using Basalt.BedrockProtocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;

public sealed class ObserverTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.Observer.ToIdentifier()];

    private const string StateFacing = "minecraft:facing_direction";
    private const string StatePowered = "powered_bit";

    public ObserverTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) return;

        FacingDirection facing = details.Player.Pitch switch {
            > 45f => FacingDirection.Down,
            < -45f => FacingDirection.Up,
            _ => RotationComponent.GetCardinalDirection(details.Player.Yaw) switch {
                CardinalDirection.North => FacingDirection.North,
                CardinalDirection.South => FacingDirection.South,
                CardinalDirection.East => FacingDirection.East,
                _ => FacingDirection.West
            }
        };

        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StateFacing] = facing switch {
            FacingDirection.Down => "down",
            FacingDirection.Up => "up",
            FacingDirection.North => "north",
            FacingDirection.South => "south",
            FacingDirection.West => "west",
            _ => "east"
        };
        state[StatePowered] = false;
        dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
    }

    public override void OnTick(BlockTickDetails details) {
        bool powered = IsPowered(Block.Permutation);
        SetPowered(details.Dimension, details.BlockPosition, !powered);
        if (!powered) {
            details.Dimension.ScheduleBlockTick(details.BlockPosition, 2);
        }
    }

    public void OnObservedBlockChanged(Dimension dimension, BlockPos observerPosition, BlockPos changedPosition) {
        if (IsPowered(Block.Permutation) || !IsWatching(Block.Permutation, observerPosition, changedPosition)) {
            return;
        }

        dimension.ScheduleBlockTick(observerPosition, 2);
    }

    public static bool Powers(BlockPermutation observer, BlockPos observerPosition, BlockPos targetPosition) {
        if (observer.Type.Identifier != BlockIdentifier.Observer.ToIdentifier() || !IsPowered(observer)) {
            return false;
        }

        FacingDirection output = Opposite(GetFacing(observer));
        BlockPos outputPosition = Offset(observerPosition, output);
        return outputPosition.X == targetPosition.X &&
            outputPosition.Y == targetPosition.Y &&
            outputPosition.Z == targetPosition.Z;
    }

    private void SetPowered(Dimension dimension, BlockPos position, bool powered) {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StatePowered] = powered;
        dimension.SetPermutation(
            position.X,
            position.Y,
            position.Z,
            Block.Type.GetPermutation(state));
    }

    private static bool IsWatching(BlockPermutation observer, BlockPos observerPosition, BlockPos changedPosition) {
        BlockPos watchedPosition = Offset(observerPosition, GetFacing(observer));
        return watchedPosition.X == changedPosition.X &&
            watchedPosition.Y == changedPosition.Y &&
            watchedPosition.Z == changedPosition.Z;
    }

    public static bool IsPowered(BlockPermutation observer) {
        return observer.State.TryGetValue(StatePowered, out BlockStateValue value) &&
            value.Kind == 2 && value.AsBool();
    }

    private static FacingDirection GetFacing(BlockPermutation observer) {
        if (!observer.State.TryGetValue(StateFacing, out BlockStateValue value) || value.Kind != 1) {
            return FacingDirection.South;
        }

        return value.AsString() switch {
            "down" => FacingDirection.Down,
            "up" => FacingDirection.Up,
            "north" => FacingDirection.North,
            "south" => FacingDirection.South,
            "west" => FacingDirection.West,
            "east" => FacingDirection.East,
            _ => FacingDirection.South
        };
    }

    private static FacingDirection Opposite(FacingDirection facing) {
        return facing switch {
            FacingDirection.Down => FacingDirection.Up,
            FacingDirection.Up => FacingDirection.Down,
            FacingDirection.North => FacingDirection.South,
            FacingDirection.South => FacingDirection.North,
            FacingDirection.West => FacingDirection.East,
            _ => FacingDirection.West
        };
    }

    private static BlockPos Offset(BlockPos position, FacingDirection direction) {
        return direction switch {
            FacingDirection.Down => new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z },
            FacingDirection.Up => new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z },
            FacingDirection.North => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            FacingDirection.South => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            FacingDirection.West => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z },
            _ => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z }
        };
    }
}
