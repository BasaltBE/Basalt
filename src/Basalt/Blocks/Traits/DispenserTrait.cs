namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class DispenserTrait : BlockTrait {
    public static new readonly string[] Types = [
        BlockIdentifier.Dispenser.ToIdentifier(),
        BlockIdentifier.Dropper.ToIdentifier()
    ];

    private const int ContainerSize = 9;
    private BlockContainer? _container;
    private bool _powered;

    public override bool Interactable => true;

    public DispenserTrait(Block block) : base(block) {
    }

    public BlockContainer? Container => _container;

    public override void OnRead(CompoundTag tag) {
        _powered = tag.Get<ByteTag>("powered")?.Value == 1;
        if (tag.Get<ListTag>("Items") is not { } items) {
            return;
        }

        EnsureContainer(null, 0, 0, 0);
        foreach (BaseTag value in items.Values) {
            if (value is not CompoundTag itemTag) {
                continue;
            }

            int slot = itemTag.Get<ByteTag>("Slot")?.Value ?? -1;
            if (_container is null || slot < 0 || slot >= _container.GetSize()) {
                continue;
            }

            ItemStack? item = ItemStack.Deserialize(itemTag);
            if (item is not null) {
                _container.SetItem(slot, item);
            }
        }
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("powered", new ByteTag { Value = _powered ? (sbyte)1 : (sbyte)0 });
        if (_container is null) {
            return;
        }

        ListTag items = new() { Name = "Items" };
        for (int slot = 0; slot < _container.GetSize(); slot++) {
            ItemStack? item = _container.GetItem(slot);
            if (item is null || item.StackSize == 0) {
                continue;
            }

            CompoundTag itemTag = item.Serialize();
            itemTag.Set("Slot", new ByteTag { Value = (sbyte)slot });
            items.Values.Add(itemTag);
        }

        if (items.Values.Count > 0) {
            tag.Set("Items", items);
        }
    }

    public override void OnInteract(BlockInteractDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        EnsureContainer(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);
        _container?.Show(details.Player);
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        EnsureContainer(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);
        _powered = IsPowered(dimension, details.BlockPosition);
        if (_powered) {
            dimension.ScheduleBlockTick(details.BlockPosition, 1);
        }
    }

    public override void OnBreak(BlockBreakDetails details) {
        if (_container is null) {
            return;
        }

        foreach ((Player.Player player, _) in _container.GetAllOccupants().ToList()) {
            _container.Close(player);
        }

        Dimension? dimension = details.Player.Dimension;
        if (dimension is not null) {
            ulong currentTick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
            for (int slot = 0; slot < _container.GetSize(); slot++) {
                ItemStack? item = _container.GetItem(slot);
                if (item is null || item.StackSize == 0) {
                    continue;
                }

                ItemEntity drop = new(item) {
                    Position = new Vec3 {
                        X = details.BlockPosition.X + 0.5f,
                        Y = details.BlockPosition.Y + 0.5f,
                        Z = details.BlockPosition.Z + 0.5f
                    }
                };
                drop.LockPickupUntil(currentTick + 10);
                drop.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            }
        }

        _container = null;
    }

    public override void OnRender(Player.Player player, int x, int y, int z) {
        Dimension? dimension = player.Dimension;
        if (dimension is null) {
            return;
        }

        EnsureContainer(dimension, x, y, z);
        BlockPos position = new() { X = x, Y = y, Z = z };
        BlockLevelStorage? storage = dimension.GetLoadedChunk(x >> 4, z >> 4)?.GetBlockStorage(position);
        if (storage is null) {
            return;
        }

        uint networkId = (uint)dimension.GetLoadedPermutationOrAir(x, y, z).NetworkId;
        player.Send(
            new BlockActorDataPacket { Position = position, ActorData = storage },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = 0,
                Flags = (uint)Basalt.Core.Blocks.UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = networkId,
                Flags = (uint)Basalt.Core.Blocks.UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            });
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        bool powered = IsPowered(details.Dimension, details.BlockPosition);
        if (powered == _powered) {
            return;
        }

        _powered = powered;
        if (powered) {
            details.Dimension.ScheduleBlockTick(details.BlockPosition, 1);
        }
    }

    public override void OnTick(BlockTickDetails details) {
        if (!_powered || _container is null) {
            return;
        }

        int slot = Random.Shared.Next(_container.GetSize());
        for (int offset = 0; offset < _container.GetSize(); offset++) {
            int candidate = (slot + offset) % _container.GetSize();
            if (_container.GetItem(candidate) is { StackSize: > 0 }) {
                ItemStack? item = _container.TakeItem(candidate, 1);
                if (item is not null) {
                    SpawnItem(details.Dimension, details.BlockPosition, item);
                }
                return;
            }
        }
    }

    private void EnsureContainer(Dimension? dimension, int x, int y, int z) {
        if (_container is not null) {
            if (dimension is not null && _container.Dimension is null) {
                _container.Dimension = dimension;
                _container.Position = new BlockPos { X = x, Y = y, Z = z };
            }
            return;
        }

        ContainerType type = Block.Type.Identifier == BlockIdentifier.Dropper.ToIdentifier()
            ? ContainerType.DROPPER
            : ContainerType.DISPENSER;
        _container = new BlockContainer(
            dimension,
            new BlockPos { X = x, Y = y, Z = z },
            type,
            ContainerSize) {
            OnContainerUpdated = OnContainerUpdated
        };
    }

    private void OnContainerUpdated(BlockContainer container) {
        if (container.Dimension is null) {
            return;
        }

        var chunk = container.Dimension.GetLoadedChunk(
            container.Position.X >> 4,
            container.Position.Z >> 4);
        if (chunk is not null) {
            chunk.Dirty = true;
        }
    }

    private void SpawnItem(Dimension dimension, BlockPos position, ItemStack item) {
        FacingDirection facing = GetFacing();
        (int x, int y, int z) = facing switch {
            FacingDirection.Down => (0, -1, 0),
            FacingDirection.Up => (0, 1, 0),
            FacingDirection.North => (0, 0, -1),
            FacingDirection.South => (0, 0, 1),
            FacingDirection.West => (-1, 0, 0),
            _ => (1, 0, 0)
        };

        ItemEntity entity = new(item) {
            Position = new Vec3 {
                X = position.X + 0.5f + x * 0.7f,
                Y = position.Y + 0.5f + y * 0.7f,
                Z = position.Z + 0.5f + z * 0.7f
            },
            Velocity = new Vec3 {
                X = x * 0.2f,
                Y = y * 0.2f + 0.1f,
                Z = z * 0.2f
            }
        };
        ulong currentTick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
        entity.LockPickupUntil(currentTick + 30);
        entity.LockMergeUntil(currentTick + 30);
        entity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
    }

    private FacingDirection GetFacing() {
        if (!Block.Permutation.State.TryGetValue("facing_direction", out BlockStateValue value) || value.Kind != 0) {
            return FacingDirection.North;
        }

        return (FacingDirection)Math.Clamp((int)value.AsNumber(), 0, 5);
    }

    private static bool IsPowered(Dimension dimension, BlockPos position) {
        ReadOnlySpan<(int X, int Y, int Z)> offsets = [
            (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)
        ];

        for (int i = 0; i < offsets.Length; i++) {
            (int x, int y, int z) = offsets[i];
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

            if (neighbor.Type.Identifier == BlockIdentifier.PoweredRepeater.ToIdentifier()) {
                return true;
            }

            if (neighbor.Type.GetComponent<RedstoneProducerComponent>() is { Power: > 0 }) {
                return true;
            }
        }

        return false;
    }
}
