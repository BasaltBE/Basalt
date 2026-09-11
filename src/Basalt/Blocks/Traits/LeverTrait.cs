namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class LeverTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.Lever.ToIdentifier()];

    private const string StateDirection = "lever_direction";
    private const string StateOpen = "open_bit";

    public override bool Interactable => true;

    public LeverTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) return;

        // TODO: maybe have an enum for face or sum or just reuse 
        BlockPos supportPosition = details.BlockFace switch {
            0 => new BlockPos { X = details.BlockPosition.X, Y = details.BlockPosition.Y + 1, Z = details.BlockPosition.Z },
            1 => new BlockPos { X = details.BlockPosition.X, Y = details.BlockPosition.Y - 1, Z = details.BlockPosition.Z },
            2 => new BlockPos { X = details.BlockPosition.X, Y = details.BlockPosition.Y, Z = details.BlockPosition.Z + 1 },
            3 => new BlockPos { X = details.BlockPosition.X, Y = details.BlockPosition.Y, Z = details.BlockPosition.Z - 1 },
            4 => new BlockPos { X = details.BlockPosition.X + 1, Y = details.BlockPosition.Y, Z = details.BlockPosition.Z },
            5 => new BlockPos { X = details.BlockPosition.X - 1, Y = details.BlockPosition.Y, Z = details.BlockPosition.Z },
            _ => details.ClickedPosition
        };

        BlockPermutation support = dimension.GetLoadedPermutationOrAir(
            supportPosition.X,
            supportPosition.Y,
            supportPosition.Z);
        if (!support.Type.Solid) {
            dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        CardinalDirection playerDirection = RotationComponent.GetCardinalDirection(details.Player.Yaw);
        // TODO: enum here is prob necesary :sob:
        string direction = details.BlockFace switch {
            0 => playerDirection is CardinalDirection.East or CardinalDirection.West
                ? "down_east_west"
                : "down_north_south",
            1 => playerDirection is CardinalDirection.East or CardinalDirection.West
                ? "up_east_west"
                : "up_north_south",
            2 => "north",
            3 => "south",
            4 => "west",
            5 => "east",
            _ => "up_north_south"
        };

        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StateDirection] = direction;
        state[StateOpen] = false;
        Block.SetPermutation(Block.Type.GetPermutation(state));
        dimension.UpdateRedstone(details.BlockPosition);
    }

    public override void OnInteract(BlockInteractDetails details) {
        Dimension dimension = details.Player.Dimension!;
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        bool open = Block.Permutation.State.TryGetValue(StateOpen, out BlockStateValue openValue) &&
            openValue.Kind == 2 && openValue.AsBool();
        state[StateOpen] = !open;
        dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
        dimension.UpdateRedstone(details.BlockPosition);
    }

    public override void OnTick(BlockTickDetails details) {
        if (!HasSupport(details.Dimension, details.BlockPosition)) {
            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
        }
    }

    private bool HasSupport(Dimension dimension, BlockPos position) {
        string direction = Block.Permutation.State.TryGetValue(StateDirection, out BlockStateValue value) && value.Kind == 1
            ? value.AsString()
            : "up_north_south";

        BlockPos support = direction switch {
            "east" => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z },
            "west" => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z },
            "north" => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            "south" => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            "down_east_west" or "down_north_south" => new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z },
            _ => new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z }
        };

        return dimension.GetLoadedPermutationOrAir(support.X, support.Y, support.Z).Type.Solid;
    }
}
