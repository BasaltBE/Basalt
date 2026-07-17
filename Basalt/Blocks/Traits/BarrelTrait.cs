namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities;
using Basalt.Core.Item;
using Basalt.Core.Worlds;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;


public class BarrelTrait : BlockTrait
{
    public override bool Interactable => true;
    public static new readonly string Identifier = "minecraft:barrel";
    public static new readonly string[] Types = ["minecraft:barrel"];

    private BlockContainer? _container;

    public BarrelTrait(Block block) : base(block)
    {
    }

    public BlockContainer? Container => _container;

    public override void OnRead(CompoundTag tag)
    {
        if (tag.Get<ListTag>("Items") is not { } items)
        {
            return;
        }

        EnsureContainer(null, 0, 0, 0);

        foreach (BaseTag tagItem in items.Values)
        {
            if (tagItem is not CompoundTag itemTag)
            {
                continue;
            }

            int slot = itemTag.Get<ByteTag>("Slot") is { } slotTag
                ? (byte)slotTag.Value
                : 0;

            if (_container is null || slot < 0 || slot >= _container.GetSize())
            {
                continue;
            }

            ItemStack? item = ItemStack.Deserialize(itemTag);
            if (item is not null)
            {
                _container.SetItem(slot, item);
            }
        }
    }

    public override void OnWrite(CompoundTag tag)
    {
        if (_container is null)
        {
            return;
        }

        ListTag items = new() { Name = "Items" };

        for (int slot = 0; slot < _container.GetSize(); slot++)
        {
            ItemStack? item = _container.GetItem(slot);
            if (item is null || item.StackSize == 0)
            {
                continue;
            }

            CompoundTag itemTag = item.Serialize();
            itemTag.Set("Slot", new ByteTag { Value = (sbyte)slot });
            items.Values.Add(itemTag);
        }

        if (items.Values.Count > 0)
        {
            tag.Set("Items", items);
        }
    }

    public override void OnInteract(BlockInteractDetails details)
    {
        var dimension = details.Player.Dimension;
        if (dimension is null)
        {
            return;
        }

        EnsureContainer(dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
        _container?.Show(details.Player);
    }

    public override void OnBreak(BlockBreakDetails details)
    {
        if (_container is null)
        {
            return;
        }

        foreach ((Basalt.Core.Player.Player player, _) in _container.GetAllOccupants().ToList())
        {
            _container.Close(player);
        }

        var dimension = details.Player.Dimension;
        if (dimension is not null)
        {
            ulong currentTick = dimension.World is Worlds.Tickable tickable ? tickable.TickValue : 0;

            for (int i = 0; i < _container.GetSize(); i++)
            {
                Item.ItemStack? item = _container.GetItem(i);
                if (item is null || item.StackSize == 0) continue;

                Entities.ItemEntity drop = new(item)
                {
                    Position = new Protocol.Types.Vec3f
                    {
                        X = details.BlockPosition.X + 0.5f,
                        Y = details.BlockPosition.Y + 0.5f,
                        Z = details.BlockPosition.Z + 0.5f
                    }
                };

                drop.LockPickupUntil(currentTick + 10);
                drop.Spawn(dimension, new Entities.Traits.Types.EntitySpawnOptions(InitialSpawn: false));
            }
        }

        _container = null;
    }

    public override void OnRender(Player.Player player, int x, int y, int z)
    {
        var dimension = player.Dimension;
        if (dimension is null)
        {
            return;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };

        EnsureContainer(dimension, x, y, z);
        WriteStorage(dimension, x, y, z);

        BlockLevelStorage? storage = dimension
            .GetChunk(x >> 4, z >> 4)
            ?.GetBlockStorage(position);

        if (storage is null)
        {
            return;
        }

        uint networkId = (uint)dimension.GetPermutation(x, y, z).NetworkId;

        player.Send(
        new BlockActorDataPacket
        {
            Position = position,
            Data = storage
        },
        new UpdateBlockPacket
        {
            Position = position,
            NetworkBlockId = 0,
            Flags = UpdateBlockFlagsType.None,
            Layer = UpdateBlockLayerType.Normal
        },
        new UpdateBlockPacket
        {
            Position = position,
            NetworkBlockId = networkId,
            Flags = UpdateBlockFlagsType.None,
            Layer = UpdateBlockLayerType.Normal
        });
    }

    public void Open(bool silent = false)
    {
        SetOpen(true);

        if (!silent)
        {
            BroadcastSound(LevelSoundEvent.BarrelOpen);
        }
    }

    public void Close(bool silent = false)
    {
        SetOpen(false);

        if (!silent)
        {
            BroadcastSound(LevelSoundEvent.BarrelClose);
        }
    }

    private void EnsureContainer(global::Basalt.Core.Worlds.Dimensions.Dimension? dimension, int x, int y, int z)
    {
        if (_container is not null)
        {
            if (dimension is not null && _container.Dimension is null)
            {
                _container.Dimension = dimension;
                _container.Position = new BlockPos { X = x, Y = y, Z = z };
            }

            return;
        }

        _container = new BlockContainer(dimension, new BlockPos { X = x, Y = y, Z = z }, ContainerType.Container, 27);
        _container.OnViewerAddedEvent = OnViewerAdded;
        _container.OnViewerRemovedEvent = OnViewerRemoved;
        _container.OnContainerUpdated = OnContainerUpdated;
    }

    private void OnViewerAdded(BlockContainer container, Basalt.Core.Player.Player _)
    {
        if (container.occupants.Count == 1)
        {
            Open();
        }
    }

    private void OnViewerRemoved(BlockContainer container, Basalt.Core.Player.Player _)
    {
        if (container.occupants.Count == 0)
        {
            Close();
        }
    }

    private void SetOpen(bool open)
    {
        if (_container?.Dimension is null)
        {
            return;
        }

        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State)
        {
            state[key] = value;
        }

        state["open_bit"] = open;

        BlockPermutation permutation = Block.Type.GetPermutation(state);
        Block.SetPermutation(permutation);
        _container.Dimension.SetPermutation(_container.Position.X, _container.Position.Y, _container.Position.Z, permutation);
        // _container.Dimension.Broadcast(new UpdateBlockPacket
        // {
        //     Position = _container.Position,
        //     NetworkBlockId = (uint)permutation.NetworkId,
        //     Flags = UpdateBlockFlagsType.Network,
        //     Layer = UpdateBlockLayerType.Normal
        // });
    }

    private void BroadcastSound(string soundEvent)
    {
        if (_container?.Dimension is null)
        {
            return;
        }

        _container.Dimension.Broadcast(new LevelSoundEventPacket
        {
            Event = soundEvent,
            Position = new Vec3f
            {
                X = _container.Position.X,
                Y = _container.Position.Y,
                Z = _container.Position.Z
            },
            Data = Block.Permutation.NetworkId,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = -1
        });
    }

    private void OnContainerUpdated(BlockContainer container)
    {
        if (container.Dimension is null)
        {
            return;
        }

        var chunk = container.Dimension.GetChunk(container.Position.X >> 4, container.Position.Z >> 4);
        if (chunk is not null)
        {
            chunk.Dirty = true;
        }
    }

    private void WriteStorage(global::Basalt.Core.Worlds.Dimensions.Dimension dimension, int x, int y, int z)
    {
        var chunk = dimension.GetChunk(x >> 4, z >> 4);
        if (chunk is null)
        {
            return;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };
        BlockLevelStorage? storage = chunk.GetBlockStorage(position);
        if (storage is null)
        {
            storage = new BlockLevelStorage(chunk);
            storage.SetPosition(position);
            storage.Set("id", new StringTag { Name = "id", Value = "Barrel" });
            storage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
        }

        OnWrite(storage);
        chunk.SetBlockStorage(position, storage, dirty: true);
    }
}







