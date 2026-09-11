namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class RedstoneWireTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.RedstoneWire.ToIdentifier()];

    private const string StateSignal = "redstone_signal";

    public RedstoneWireTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        details.Player.Dimension?.UpdateRedstone(details.BlockPosition);
    }

    public override void OnTick(BlockTickDetails details) {
        BlockPermutation current = Block.Permutation;
        BlockPermutation below = details.Dimension.GetLoadedPermutationOrAir(
            details.BlockPosition.X,
            details.BlockPosition.Y - 1,
            details.BlockPosition.Z);
        if (!below.Type.Solid) {
            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        int signal = GetSignal(details.Dimension, details.BlockPosition);
        int currentSignal = current.State.TryGetValue(StateSignal, out BlockStateValue value) && value.Kind == 0
            ? Math.Clamp((int)value.AsNumber(), 0, 15)
            : 0;
        if (signal == currentSignal) return;

        BlockState state = [];
        foreach ((string key, BlockStateValue stateValue) in current.State) {
            state[key] = stateValue;
        }

        state[StateSignal] = signal;
        details.Dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
    }

    private static int GetSignal(Dimension dimension, BlockPos position) {
        ReadOnlySpan<(int X, int Y, int Z)> offsets = [
            (1, 0, 0),
            (-1, 0, 0),
            (0, 0, 1),
            (0, 0, -1),
            (0, 1, 0),
            (0, -1, 0)
        ];

        int signal = 0;
        for (int i = 0; i < offsets.Length; i++) {
            (int x, int y, int z) = offsets[i];
            BlockPos neighborPosition = new() {
                X = position.X + x,
                Y = position.Y + y,
                Z = position.Z + z
            };
            BlockPermutation neighbor = dimension.GetLoadedPermutationOrAir(
                neighborPosition.X,
                neighborPosition.Y,
                neighborPosition.Z);

            int candidate = GetNeighborSignal(neighbor, position, neighborPosition);
            if (candidate > signal) signal = candidate;
            if (signal == 15) return signal;

            if (y == 0 && x != 0 || y == 0 && z != 0) {
                BlockPos raisedPosition = new() {
                    X = neighborPosition.X,
                    Y = neighborPosition.Y + 1,
                    Z = neighborPosition.Z
                };
                BlockPermutation raised = dimension.GetLoadedPermutationOrAir(
                    raisedPosition.X,
                    raisedPosition.Y,
                    raisedPosition.Z);
                if (raised.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
                    dimension.GetLoadedPermutationOrAir(neighborPosition.X, neighborPosition.Y, neighborPosition.Z).Type.Solid) {
                    candidate = GetNeighborSignal(raised, position, raisedPosition);
                    if (candidate > signal) signal = candidate;
                }

                BlockPos loweredPosition = new() {
                    X = neighborPosition.X,
                    Y = neighborPosition.Y - 1,
                    Z = neighborPosition.Z
                };
                BlockPermutation lowered = dimension.GetLoadedPermutationOrAir(
                    loweredPosition.X,
                    loweredPosition.Y,
                    loweredPosition.Z);
                if (lowered.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
                    dimension.GetLoadedPermutationOrAir(
                        loweredPosition.X,
                        loweredPosition.Y - 1,
                        loweredPosition.Z).Type.Solid) {
                    candidate = GetNeighborSignal(lowered, position, loweredPosition);
                    if (candidate > signal) signal = candidate;
                }
            }
        }

        return signal;
    }

    private static int GetNeighborSignal(BlockPermutation neighbor, BlockPos position, BlockPos neighborPosition) {
        if (neighbor.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
            neighbor.State.TryGetValue(StateSignal, out BlockStateValue wireSignal) &&
            wireSignal.Kind == 0) {
            return Math.Clamp((int)wireSignal.AsNumber() - 1, 0, 15);
        }

        if (neighbor.Type.GetComponent<RedstoneProducerComponent>() is { Power: > 0 } producer) {
            return Math.Clamp(producer.Power, 0, 15);
        }

        if (neighbor.Type.Identifier.EndsWith("_pressure_plate", StringComparison.Ordinal) &&
            neighbor.State.TryGetValue(StateSignal, out BlockStateValue plateSignal) &&
            plateSignal.Kind == 0) {
            return Math.Clamp((int)plateSignal.AsNumber(), 0, 15);
        }

        if (neighbor.Type.Identifier == BlockIdentifier.Lever.ToIdentifier() &&
            neighbor.State.TryGetValue("open_bit", out BlockStateValue leverOpen) &&
            leverOpen.Kind == 2 && leverOpen.AsBool()) {
            return 15;
        }

        if (neighbor.Type.Identifier.EndsWith("_button", StringComparison.Ordinal) &&
            neighbor.State.TryGetValue("button_pressed_bit", out BlockStateValue buttonPressed) &&
            buttonPressed.Kind == 2 && buttonPressed.AsBool()) {
            return 15;
        }

        if (neighbor.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier() &&
            neighbor.State.TryGetValue("minecraft:cardinal_direction", out BlockStateValue direction) &&
            direction.Kind == 1 && RedstonePower.PointsTo(
                RedstonePower.ParseFacing(direction.AsString()), neighborPosition, position)) {
            return 15;
        }

        if (neighbor.Type.Identifier == BlockIdentifier.Observer.ToIdentifier() &&
            ObserverTrait.Powers(neighbor, neighborPosition, position)) {
            return 15;
        }

        if (neighbor.Type.Identifier == BlockIdentifier.PoweredComparator.ToIdentifier() &&
            neighbor.State.TryGetValue("minecraft:cardinal_direction", out BlockStateValue comparatorDirection) &&
            comparatorDirection.Kind == 1 && RedstonePower.PointsTo(
                RedstonePower.ParseFacing(comparatorDirection.AsString()), neighborPosition, position)) {
            return 15;
        }

        return 0;
    }
}
