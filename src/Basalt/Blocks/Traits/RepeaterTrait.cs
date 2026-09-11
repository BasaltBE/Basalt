namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class RepeaterTrait : BlockTrait {
    public static new readonly string[] Types = [
        BlockIdentifier.PoweredRepeater.ToIdentifier(),
        BlockIdentifier.UnpoweredRepeater.ToIdentifier()
    ];

    private const string StateFacing = "minecraft:cardinal_direction";
    private const string StateDelay = "repeater_delay";
    private bool? _pendingPowered;

    public override bool Interactable => true;

    public RepeaterTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        CardinalDirection direction = RotationComponent.GetCardinalDirection(details.Player.Yaw) switch {
            CardinalDirection.North => CardinalDirection.South,
            CardinalDirection.South => CardinalDirection.North,
            CardinalDirection.East => CardinalDirection.West,
            _ => CardinalDirection.East
        };
        string facing = direction switch {
            CardinalDirection.North => "north",
            CardinalDirection.South => "south",
            CardinalDirection.East => "east",
            _ => "west"
        };
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StateFacing] = facing;
        details.Player.Dimension?.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
        details.Player.Dimension?.UpdateRedstone(details.BlockPosition);
    }

    public override void OnInteract(BlockInteractDetails details) {
        Dimension dimension = details.Player.Dimension!;
        int delayState = Block.Permutation.State.TryGetValue(StateDelay, out BlockStateValue delay) && delay.Kind == 0
            ? (int)delay.AsNumber()
            : 0;
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StateDelay] = (delayState + 1) % 4;
        dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
    }

    public override void OnTick(BlockTickDetails details) {
        bool currentPowered = Block.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier();
        bool inputPowered = HasRearPower(details.Dimension, details.BlockPosition);

        if (_pendingPowered is bool pendingPowered) {
            _pendingPowered = null;
            if (pendingPowered != inputPowered) {
                _pendingPowered = inputPowered;
                details.Dimension.ScheduleBlockTick(details.BlockPosition, (uint)GetDelay());
                return;
            }

            if (pendingPowered == currentPowered) {
                return;
            }

            BlockState state = [];
            foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
                state[key] = value;
            }

            BlockIdentifier target = pendingPowered
                ? BlockIdentifier.PoweredRepeater
                : BlockIdentifier.UnpoweredRepeater;

            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(target, state));
            return;
        }

        if (inputPowered == currentPowered) {
            return;
        }

        _pendingPowered = inputPowered;
        details.Dimension.ScheduleBlockTick(details.BlockPosition, (uint)GetDelay());
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        bool currentPowered = Block.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier();
        bool inputPowered = HasRearPower(details.Dimension, details.BlockPosition);
        if (inputPowered == currentPowered || _pendingPowered == inputPowered) return;

        _pendingPowered = inputPowered;
        details.Dimension.ScheduleBlockTick(details.BlockPosition, (uint)GetDelay());
    }

    private int GetDelay() {
        if (!Block.Permutation.State.TryGetValue(StateDelay, out BlockStateValue value) || value.Kind != 0) {
            return 1;
        }

        return Math.Clamp((int)value.AsNumber() + 1, 1, 4);
    }

    private CardinalDirection GetFacing() {
        if (!Block.Permutation.State.TryGetValue(StateFacing, out BlockStateValue value) || value.Kind != 1) {
            return CardinalDirection.South;
        }

        return value.AsString() switch {
            "north" => CardinalDirection.North,
            "south" => CardinalDirection.South,
            "east" => CardinalDirection.East,
            "west" => CardinalDirection.West,
            _ => CardinalDirection.South
        };
    }

    private static bool HasRearPower(Dimension dimension, BlockPos position) {
        Block? repeater = dimension.GetBlock(position.X, position.Y, position.Z);
        CardinalDirection facing = repeater?.GetTrait<RepeaterTrait>()?.GetFacing() ?? CardinalDirection.South;
        BlockPos rear = facing switch {
            CardinalDirection.North => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            CardinalDirection.South => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            CardinalDirection.East => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z },
            _ => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z }
        };

        BlockPermutation source = dimension.GetLoadedPermutationOrAir(rear.X, rear.Y, rear.Z);
        if (source.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier()) {
            CardinalDirection sourceFacing = source.State.TryGetValue(StateFacing, out BlockStateValue sourceDirection) && sourceDirection.Kind == 1
                ? sourceDirection.AsString() switch {
                    "north" => CardinalDirection.North,
                    "south" => CardinalDirection.South,
                    "east" => CardinalDirection.East,
                    "west" => CardinalDirection.West,
                    _ => CardinalDirection.North
                }
                : CardinalDirection.North;

            if (sourceFacing == GetDirectionFromOffset(position.X - rear.X, position.Z - rear.Z)) {
                return true;
            }

            return false;
        }

        if (source.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
            source.State.TryGetValue("redstone_signal", out BlockStateValue signal) &&
            signal.Kind == 0 && signal.AsNumber() > 0) {
            return true;
        }

        if (source.Type.Identifier == BlockIdentifier.Observer.ToIdentifier() &&
            ObserverTrait.Powers(source, rear, position)) {
            return true;
        }

        if (source.Type.GetComponent<RedstoneProducerComponent>() is { Power: > 0 }) {
            return true;
        }

        if (source.Type.Identifier == BlockIdentifier.Lever.ToIdentifier() &&
            source.State.TryGetValue("open_bit", out BlockStateValue open) && open.Kind == 2 && open.AsBool()) {
            return true;
        }

        return source.Type.Identifier.EndsWith("_button", StringComparison.Ordinal) &&
            source.State.TryGetValue("button_pressed_bit", out BlockStateValue pressed) &&
            pressed.Kind == 2 && pressed.AsBool();
    }

    private static CardinalDirection GetDirectionFromOffset(int x, int z) {
        if (x > 0) return CardinalDirection.East;
        if (x < 0) return CardinalDirection.West;
        return z > 0 ? CardinalDirection.South : CardinalDirection.North;
    }
}
