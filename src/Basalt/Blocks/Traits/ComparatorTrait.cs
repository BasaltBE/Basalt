namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class ComparatorTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.PoweredComparator.ToIdentifier(), BlockIdentifier.UnpoweredComparator.ToIdentifier()];

    private const string StateFacing = "minecraft:cardinal_direction";
    private const string StateSubtract = "output_subtract_bit";
    private const string StateLit = "output_lit_bit";

    public override bool Interactable => true;

    public ComparatorTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        BlockState state = CopyState();
        FacingDirection facing = RotationComponent.GetCardinalDirection(details.Player.Yaw) switch {
            CardinalDirection.North => FacingDirection.South,
            CardinalDirection.South => FacingDirection.North,
            CardinalDirection.East => FacingDirection.West,
            _ => FacingDirection.East
        };
        state[StateFacing] = ToState(facing);
        state[StateSubtract] = false;
        state[StateLit] = false;
        Dimension dimension = details.Player.Dimension!;
        dimension.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, Block.Type.GetPermutation(state));
        dimension.ScheduleBlockTick(details.BlockPosition, 1);
    }

    public override void OnInteract(BlockInteractDetails details) {
        BlockState state = CopyState();
        bool subtract = state.TryGetValue(StateSubtract, out BlockStateValue value) && value.Kind == 2 && value.AsBool();
        state[StateSubtract] = !subtract;
        details.Player.Dimension!.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, Block.Type.GetPermutation(state));
        details.Player.Dimension.UpdateRedstone(details.BlockPosition);
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) => details.Dimension.ScheduleBlockTick(details.BlockPosition, 1);

    public override void OnTick(BlockTickDetails details) {
        FacingDirection facing = GetFacing();
        BlockPos rear = Offset(details.BlockPosition, facing, false);
        BlockPos sideA = Offset(details.BlockPosition, Rotate(facing, true), false);
        BlockPos sideB = Offset(details.BlockPosition, Rotate(facing, false), false);
        int rearSignal = GetSignal(details.Dimension, rear, details.BlockPosition);
        int sideSignal = Math.Max(GetSignal(details.Dimension, sideA, details.BlockPosition), GetSignal(details.Dimension, sideB, details.BlockPosition));
        bool subtract = Block.Permutation.State.TryGetValue(StateSubtract, out BlockStateValue mode) && mode.Kind == 2 && mode.AsBool();
        bool powered = subtract ? rearSignal - sideSignal > 0 : rearSignal >= sideSignal && rearSignal > 0;
        bool current = Block.Type.Identifier == BlockIdentifier.PoweredComparator.ToIdentifier();
        if (powered != current) {
            BlockState state = CopyState();
            state[StateLit] = powered;
            details.Dimension.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, BlockPermutation.Resolve(powered ? BlockIdentifier.PoweredComparator : BlockIdentifier.UnpoweredComparator, state));
            details.Dimension.UpdateRedstone(details.BlockPosition);
        }
    }

    private BlockState CopyState() {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) state[key] = value;
        return state;
    }

    private FacingDirection GetFacing() => Block.Permutation.State.TryGetValue(StateFacing, out BlockStateValue value) && value.Kind == 1
        ? RedstonePower.ParseFacing(value.AsString())
        : FacingDirection.South;

    private static int GetSignal(Dimension dimension, BlockPos position, BlockPos target) => RedstonePower.GetSignal(dimension.GetLoadedPermutationOrAir(position.X, position.Y, position.Z), position, target);

    private static FacingDirection Rotate(FacingDirection facing, bool clockwise) => facing switch {
        FacingDirection.North => clockwise ? FacingDirection.East : FacingDirection.West,
        FacingDirection.South => clockwise ? FacingDirection.West : FacingDirection.East,
        FacingDirection.East => clockwise ? FacingDirection.South : FacingDirection.North,
        _ => clockwise ? FacingDirection.North : FacingDirection.South
    };

    private static BlockPos Offset(BlockPos position, FacingDirection facing, bool rear) => facing switch {
        FacingDirection.North => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + (rear ? 1 : -1) },
        FacingDirection.South => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - (rear ? 1 : -1) },
        FacingDirection.East => new BlockPos { X = position.X - (rear ? 1 : -1), Y = position.Y, Z = position.Z },
        _ => new BlockPos { X = position.X + (rear ? 1 : -1), Y = position.Y, Z = position.Z }
    };

    private static string ToState(FacingDirection facing) => facing switch {
        FacingDirection.North => "north",
        FacingDirection.South => "south",
        FacingDirection.East => "east",
        _ => "west"
    };
}
