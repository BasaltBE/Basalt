namespace Basalt.Core.Blocks.Traits;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Enums;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;

public sealed class PistonTrait : BlockTrait {
    public static new readonly string[] Types = [
        BlockIdentifier.Piston.ToIdentifier(),
        BlockIdentifier.StickyPiston.ToIdentifier()
    ];

    private const string StateFacing = "facing_direction";
    private const string StateExtended = "piston_extended";
    private bool _extended;
    private bool _animationPending;
    private bool _animationExtending;

    public PistonTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) return;

        FacingDirection facing = details.Player.Pitch switch {
            > 45f => FacingDirection.Up,
            < -45f => FacingDirection.Down,
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

        state[StateFacing] = (int)facing;
        dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
        dimension.UpdateRedstone(details.BlockPosition);
    }

    public override void OnRead(CompoundTag tag) {
        _extended = tag.Get<ByteTag>(StateExtended)?.Value == 1;
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set(StateExtended, new ByteTag { Name = StateExtended, Value = _extended ? (sbyte)1 : (sbyte)0 });
        tag.Set("facing", new IntTag { Name = "facing", Value = (int)GetFacing() });
        tag.Set("Progress", new FloatTag { Name = "Progress", Value = _extended ? 1f : 0f });
        tag.Set("LastProgress", new FloatTag { Name = "LastProgress", Value = _extended ? 1f : 0f });
        tag.Set("State", new ByteTag { Name = "State", Value = _extended ? (sbyte)2 : (sbyte)0 });
        tag.Set("NewState", new ByteTag { Name = "NewState", Value = _extended ? (sbyte)2 : (sbyte)0 });
        tag.Set("Sticky", new ByteTag {
            Name = "Sticky",
            Value = Block.Type.Identifier == BlockIdentifier.StickyPiston.ToIdentifier() ? (sbyte)1 : (sbyte)0
        });
        tag.Set("Extending", new ByteTag { Name = "Extending", Value = 0 });
        tag.Set("isMovable", new ByteTag { Name = "isMovable", Value = 0 });
        tag.Set("AttachedBlocks", new ListTag { Name = "AttachedBlocks" });
        tag.Set("BreakBlocks", new ListTag { Name = "BreakBlocks" });
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        FacingDirection facing = GetFacing();
        bool powered = IsPowered(details.Dimension, details.BlockPosition, facing);
        if (powered == _extended) return;

        if (!MovePiston(details.Dimension, details.BlockPosition, facing, powered, Block.Type.Identifier == BlockIdentifier.StickyPiston.ToIdentifier())) {
            return;
        }

        _extended = powered;
        _animationPending = true;
        _animationExtending = powered;
        details.Dimension.MarkBlockStorageDirty(details.BlockPosition);
    }

    public override void OnTick(BlockTickDetails details) {
        if (!_animationPending) return;

        _animationPending = false;
        UpdatePistonArmAnimation(
            details.Dimension,
            details.BlockPosition,
            GetFacing(),
            Block.Type.Identifier == BlockIdentifier.StickyPiston.ToIdentifier(),
            _animationExtending,
            _animationExtending ? 1f : 0f,
            _animationExtending ? 2 : 0);
    }

    public override void OnBreak(BlockBreakDetails details) {
        if (!_extended) return;

        if (details.Player.Dimension is { } dimension) {
            MovePiston(
                dimension,
                details.BlockPosition,
                GetFacing(),
                extending: false,
                Block.Type.Identifier == BlockIdentifier.StickyPiston.ToIdentifier());
        }
    }

    private static bool MovePiston(
        Dimension dimension,
        BlockPos position,
        FacingDirection facing,
        bool extending,
        bool sticky) {
        FacingDirection pushFacing = facing switch {
            FacingDirection.North => FacingDirection.South,
            FacingDirection.South => FacingDirection.North,
            FacingDirection.East => FacingDirection.West,
            FacingDirection.West => FacingDirection.East,
            _ => facing
        };
        (int X, int Y, int Z) direction = pushFacing switch {
            FacingDirection.Down => (0, -1, 0),
            FacingDirection.Up => (0, 1, 0),
            FacingDirection.North => (0, 0, -1),
            FacingDirection.South => (0, 0, 1),
            FacingDirection.West => (-1, 0, 0),
            _ => (1, 0, 0)
        };
        BlockPos headPosition = new() {
            X = position.X + direction.X,
            Y = position.Y + direction.Y,
            Z = position.Z + direction.Z
        };

        if (!extending) {
            UpdatePistonArmAnimation(dimension, position, pushFacing, sticky, false, 1f, 3);
            dimension.ScheduleBlockTick(position, 2);
            dimension.SetPermutation(headPosition.X, headPosition.Y, headPosition.Z, BlockPermutation.Resolve(BlockIdentifier.Air));
            dimension.Broadcast(new BlockEventPacket {
                Position = position,
                EventType = (int)BlockEventType.ChangeState,
                EventValue = 0
            });
            dimension.PlaySound(SoundIdentifier.PistonIn, new Vec3 {
                X = position.X + 0.5f,
                Y = position.Y + 0.5f,
                Z = position.Z + 0.5f
            });

            if (!sticky) return true;

            BlockPos pullPosition = new() {
                X = headPosition.X + direction.X,
                Y = headPosition.Y + direction.Y,
                Z = headPosition.Z + direction.Z
            };
            BlockPermutation pulled = dimension.GetLoadedPermutationOrAir(pullPosition.X, pullPosition.Y, pullPosition.Z);
            MovableComponent? movable = pulled.Type.GetComponent<MovableComponent>();
            if (pulled.Type.Air || movable is null || !movable.CanBePulled) return true;

            MovePistonBlock(dimension, pullPosition, headPosition, pulled, false, position);
            return true;
        }

        List<(BlockPos Position, BlockPermutation Permutation)> blocks = [];
        List<BlockPos> breakBlocks = [];
        BlockPos current = headPosition;
        while (blocks.Count < 12) {
            BlockPermutation permutation = dimension.GetLoadedPermutationOrAir(current.X, current.Y, current.Z);
            if (permutation.Type.Air) break;

            MovableComponent? movable = permutation.Type.GetComponent<MovableComponent>();
            if (movable?.BreaksWhenPushed == true) {
                breakBlocks.Add(current);
                current = new BlockPos {
                    X = current.X + direction.X,
                    Y = current.Y + direction.Y,
                    Z = current.Z + direction.Z
                };
                continue;
            }

            if (movable is null || !movable.CanBePushed) return false;
            blocks.Add((current, permutation));
            current = new BlockPos {
                X = current.X + direction.X,
                Y = current.Y + direction.Y,
                Z = current.Z + direction.Z
            };
        }

        if (!dimension.GetLoadedPermutationOrAir(current.X, current.Y, current.Z).Type.Air) return false;

        List<BlockPos> attachedBlocks = [];
        for (int i = 0; i < blocks.Count; i++) attachedBlocks.Add(blocks[i].Position);
        UpdatePistonArmAnimation(dimension, position, pushFacing, sticky, true, 0f, 1, attachedBlocks);
        dimension.ScheduleBlockTick(position, 2);

        for (int i = 0; i < breakBlocks.Count; i++) {
            BlockPos breakPosition = breakBlocks[i];
            DropPoppedBlock(dimension, breakPosition);
            dimension.SetPermutation(breakPosition.X, breakPosition.Y, breakPosition.Z, BlockPermutation.Resolve(BlockIdentifier.Air));
        }

        for (int i = blocks.Count - 1; i >= 0; i--) {
            (BlockPos source, BlockPermutation permutation) = blocks[i];
            BlockPos target = new() {
                X = source.X + direction.X,
                Y = source.Y + direction.Y,
                Z = source.Z + direction.Z
            };
            MovePistonBlock(dimension, source, target, permutation, true, position);
        }

        BlockState headState = [];
        headState["facing_direction"] = (int)pushFacing;
        dimension.SetPermutation(
            headPosition.X,
            headPosition.Y,
            headPosition.Z,
            BlockPermutation.Resolve(sticky ? BlockIdentifier.StickyPistonArmCollision : BlockIdentifier.PistonArmCollision, headState));
        dimension.Broadcast(new BlockEventPacket {
            Position = position,
            EventType = (int)BlockEventType.ChangeState,
            EventValue = 1
        });
        dimension.PlaySound(SoundIdentifier.PistonOut, new Vec3 {
            X = position.X + 0.5f,
            Y = position.Y + 0.5f,
            Z = position.Z + 0.5f
        });
        return true;
    }

    private static void DropPoppedBlock(Dimension dimension, BlockPos position) {
        Block? block = dimension.GetBlock(position.X, position.Y, position.Z);
        if (block is null) return;

        ulong currentTick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
        foreach (Item.ItemStack item in block.GetDrops()) {
            ItemEntity entity = new(item) {
                Position = new Vec3 {
                    X = position.X + 0.5f,
                    Y = position.Y + 0.5f,
                    Z = position.Z + 0.5f
                },
                Velocity = new Vec3 {
                    X = (Random.Shared.NextSingle() - 0.5f) * 0.12f,
                    Y = 0.2f + Random.Shared.NextSingle() * 0.08f,
                    Z = (Random.Shared.NextSingle() - 0.5f) * 0.12f
                }
            };
            entity.LockPickupUntil(currentTick + 10);
            entity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
        }
    }

    private static void MovePistonBlock(
        Dimension dimension,
        BlockPos source,
        BlockPos target,
        BlockPermutation permutation,
        bool expanding,
        BlockPos pistonPosition) {
        var sourceChunk = dimension.GetOrCreateChunk(source.X >> 4, source.Z >> 4);
        var targetChunk = dimension.GetOrCreateChunk(target.X >> 4, target.Z >> 4);
        BlockLevelStorage? storage = sourceChunk.GetBlockStorage(source);
        BlockPermutation movedPermutation = permutation;
        CompoundTag? movedStorage = null;
        if (permutation.Type.Identifier == BlockIdentifier.MovingBlock.ToIdentifier()) {
            int runtimeId = storage?.Get<IntTag>("moving_block_runtime")?.Value ?? 0;
            if (runtimeId == 0) return;

            movedPermutation = BlockPermutation.Resolve(runtimeId);
            movedStorage = storage?.Get<CompoundTag>("moving_block_storage")?.Clone();
        }

        dimension.SetPermutation(target.X, target.Y, target.Z, BlockPermutation.Resolve(BlockIdentifier.MovingBlock));
        BlockLevelStorage movingStorage = new(targetChunk);
        movingStorage.Set("id", new StringTag { Name = "id", Value = "MovingBlock" });
        movingStorage.Set("moving_block_runtime", new IntTag { Name = "moving_block_runtime", Value = movedPermutation.NetworkId });
        movingStorage.Set("expanding", new ByteTag { Name = "expanding", Value = expanding ? (sbyte)1 : (sbyte)0 });
        movingStorage.Set("pistonPosX", new IntTag { Name = "pistonPosX", Value = pistonPosition.X });
        movingStorage.Set("pistonPosY", new IntTag { Name = "pistonPosY", Value = pistonPosition.Y });
        movingStorage.Set("pistonPosZ", new IntTag { Name = "pistonPosZ", Value = pistonPosition.Z });
        movingStorage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
        movingStorage.Set("movingBlock", BlockPermutation.ToCompound(movedPermutation));
        movingStorage.Set("movingBlockExtra", BlockPermutation.ToCompound(BlockPermutation.Resolve(BlockIdentifier.Air)));
        if (movedStorage is not null) {
            movingStorage.Set("moving_block_storage", movedStorage);
        }
        else if (permutation.Type.Identifier != BlockIdentifier.MovingBlock.ToIdentifier() && storage is not null) {
            movingStorage.Set("moving_block_storage", storage.Clone());
        }
        dimension.CancelBlockTick(target);
        targetChunk.SetBlockStorage(target, movingStorage);
        CompoundTag actorData = new();
        foreach ((string key, BaseTag value) in movingStorage.Values) actorData.Values[key] = value;
        dimension.Broadcast(new BlockActorDataPacket { Position = target, ActorData = actorData });
        dimension.ScheduleBlockTick(target, 2);
        dimension.SetPermutation(source.X, source.Y, source.Z, BlockPermutation.Resolve(BlockIdentifier.Air));
    }

    private static void UpdatePistonArmAnimation(
        Dimension dimension,
        BlockPos position,
        FacingDirection facing,
        bool sticky,
        bool extending,
        float progress,
        int state,
        List<BlockPos>? attachedBlocks = null) {
        CompoundTag actorData = new();
        actorData.Set("id", new StringTag { Name = "id", Value = "PistonArm" });
        actorData.Set("x", new IntTag { Name = "x", Value = position.X });
        actorData.Set("y", new IntTag { Name = "y", Value = position.Y });
        actorData.Set("z", new IntTag { Name = "z", Value = position.Z });
        actorData.Set("facing", new IntTag { Name = "facing", Value = (int)facing });
        actorData.Set("Progress", new FloatTag { Name = "Progress", Value = progress });
        actorData.Set("LastProgress", new FloatTag { Name = "LastProgress", Value = progress });
        actorData.Set("State", new ByteTag { Name = "State", Value = (sbyte)state });
        actorData.Set("NewState", new ByteTag { Name = "NewState", Value = (sbyte)state });
        actorData.Set("Sticky", new ByteTag { Name = "Sticky", Value = sticky ? (sbyte)1 : (sbyte)0 });
        actorData.Set("Extending", new ByteTag { Name = "Extending", Value = extending ? (sbyte)1 : (sbyte)0 });
        actorData.Set("isMovable", new ByteTag { Name = "isMovable", Value = 0 });
        ListTag attached = new() { Name = "AttachedBlocks" };
        if (attachedBlocks is not null) {
            for (int i = 0; i < attachedBlocks.Count; i++) {
                BlockPos attachedBlock = attachedBlocks[i];
                attached.Values.Add(new IntTag { Value = attachedBlock.X });
                attached.Values.Add(new IntTag { Value = attachedBlock.Y });
                attached.Values.Add(new IntTag { Value = attachedBlock.Z });
            }
        }
        actorData.Set("AttachedBlocks", attached);
        actorData.Set("BreakBlocks", new ListTag { Name = "BreakBlocks" });
        dimension.Broadcast(new BlockActorDataPacket {
            Position = position,
            ActorData = actorData
        });
    }

    private FacingDirection GetFacing() {
        if (!Block.Permutation.State.TryGetValue(StateFacing, out BlockStateValue value) || value.Kind != 0) {
            return FacingDirection.North;
        }

        return (FacingDirection)Math.Clamp((int)value.AsNumber(), 0, 5);
    }

    private static bool IsPowered(Dimension dimension, BlockPos position, FacingDirection facing) {
        FacingDirection pushFacing = facing switch {
            FacingDirection.North => FacingDirection.South,
            FacingDirection.South => FacingDirection.North,
            FacingDirection.East => FacingDirection.West,
            FacingDirection.West => FacingDirection.East,
            _ => facing
        };
        ReadOnlySpan<(int X, int Y, int Z)> offsets = [
            (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)
        ];

        for (int i = 0; i < offsets.Length; i++) {
            (int x, int y, int z) = offsets[i];
            if (IsPushDirection(pushFacing, x, y, z)) continue;
            BlockPermutation neighbor = dimension.GetLoadedPermutationOrAir(
                position.X + x,
                position.Y + y,
                position.Z + z);

            if (neighbor.Type.Identifier == BlockIdentifier.RedstoneWire.ToIdentifier() &&
                neighbor.State.TryGetValue("redstone_signal", out BlockStateValue signal) &&
                signal.Kind == 0 && signal.AsNumber() > 0) {
                return true;
            }

            if (neighbor.Type.Identifier == BlockIdentifier.Lever.ToIdentifier() &&
                neighbor.State.TryGetValue("open_bit", out BlockStateValue lever) &&
                lever.Kind == 2 && lever.AsBool()) {
                return true;
            }

            if (neighbor.Type.Identifier.EndsWith("_button", StringComparison.Ordinal) &&
                neighbor.State.TryGetValue("button_pressed_bit", out BlockStateValue button) &&
                button.Kind == 2 && button.AsBool()) {
                return true;
            }

            if (neighbor.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier() &&
                neighbor.State.TryGetValue("minecraft:cardinal_direction", out BlockStateValue repeaterDirection) &&
                repeaterDirection.Kind == 1 &&
                PointsTo(repeaterDirection.AsString(), position, new BlockPos {
                    X = position.X + x,
                    Y = position.Y + y,
                    Z = position.Z + z
                })) {
                return true;
            }

            if (neighbor.Type.Identifier == BlockIdentifier.Observer.ToIdentifier() &&
                ObserverTrait.Powers(neighbor, new BlockPos {
                    X = position.X + x,
                    Y = position.Y + y,
                    Z = position.Z + z
                }, position)) {
                return true;
            }

            if (neighbor.Type.GetComponent<RedstoneProducerComponent>() is { Power: > 0 }) {
                return true;
            }
        }

        return false;
    }

    private static bool IsPushDirection(FacingDirection facing, int x, int y, int z) {
        return facing switch {
            FacingDirection.Down => y < 0,
            FacingDirection.Up => y > 0,
            FacingDirection.North => z < 0,
            FacingDirection.South => z > 0,
            FacingDirection.West => x < 0,
            FacingDirection.East => x > 0,
            _ => false
        };
    }

    private static bool PointsTo(string direction, BlockPos target, BlockPos source) {
        return direction switch {
            "north" => target.X == source.X && target.Z == source.Z - 1,
            "south" => target.X == source.X && target.Z == source.Z + 1,
            "east" => target.X == source.X + 1 && target.Z == source.Z,
            "west" => target.X == source.X - 1 && target.Z == source.Z,
            _ => false
        };
    }
}
