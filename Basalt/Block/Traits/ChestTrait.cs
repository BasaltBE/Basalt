using Basalt.Block.Container;
using Basalt.Block.Traits.Types;
using Basalt.Block.Types;
using Basalt.Containers;
using Basalt.Item;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Block.Traits;

public class ChestTrait : BlockTrait
{
    public static new readonly string Identifier = "minecraft:chest";
    public static new readonly string[] Types = ["minecraft:chest", "minecraft:trapped_chest"];

    private BlockContainer? _container;
    private BlockContainer? _sharedContainer;

    private int? _pairX;
    private int? _pairZ;
    private bool _isPairLead;

    public ChestTrait(Block block) : base(block)
    {
    }

    public bool IsPaired => _pairX.HasValue && _pairZ.HasValue;
    public BlockContainer? Container => _sharedContainer ?? _container;

    public override void OnRead(CompoundTag tag)
    {
        if (tag.Get<IntTag>("pairx") is { } pairX &&
            tag.Get<IntTag>("pairz") is { } pairZ)
        {
            _pairX = pairX.Value;
            _pairZ = pairZ.Value;
        }

        if (tag.Get<ByteTag>("pairlead") is { } pairLead)
        {
            _isPairLead = pairLead.Value == 1;
        }

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
        if (_pairX.HasValue)
        {
            tag.Set("pairx", new IntTag { Value = _pairX.Value });
        }

        if (_pairZ.HasValue)
        {
            tag.Set("pairz", new IntTag { Value = _pairZ.Value });
        }

        tag.Set("pairlead", new ByteTag { Value = (sbyte)(_isPairLead ? 1 : 0) });

        if (_sharedContainer is not null && _container is not null && IsPaired)
        {
            CopySharedItemsBackToSingleContainer();
        }

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

    public override void OnPlace(BlockPlaceDetails details)
    {
        var dimension = details.Player.Dimension;
        if (dimension is null)
        {
            return;
        }

        EnsureContainer(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);

        int[][] offsets = [[1, 0], [-1, 0], [0, 1], [0, -1]];

        foreach (int[] offset in offsets)
        {
            int x = details.BlockPosition.X + offset[0];
            int y = details.BlockPosition.Y;
            int z = details.BlockPosition.Z + offset[1];

            BlockPermutation neighborPermutation = dimension.GetPermutation(x, y, z);

            if (neighborPermutation.Type.Identifier != Block.Type.Identifier)
            {
                continue;
            }

            Basalt.Block.Block? neighborBlock = dimension.GetBlock(x, y, z);
            ChestTrait? neighborChest = neighborBlock?.GetTrait<ChestTrait>();

            if (neighborChest is null || neighborChest.IsPaired)
            {
                continue;
            }

            PairWith(
                neighborChest,
                details.BlockPosition.X,
                details.BlockPosition.Z,
                x,
                z);

            CheckPairing(
                dimension,
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z);

            neighborChest.CheckPairing(dimension, x, y, z);
            WriteStorage(dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
            neighborChest.WriteStorage(dimension, x, y, z);

            dimension.Broadcast(CreateBlockUpdate(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                Block.Permutation.NetworkId));

            dimension.Broadcast(CreateBlockUpdate(
                x,
                y,
                z,
                neighborPermutation.NetworkId));

            break;
        }
    }

    public override void OnInteract(BlockInteractDetails details)
    {
        var dimension = details.Player.Dimension;
        if (dimension is null)
        {
            return;
        }

        EnsureContainer(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);

        CheckPairing(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);

        if (!IsPaired)
        {
            int[][] offsets = [[1, 0], [-1, 0], [0, 1], [0, -1]];
            foreach (int[] offset in offsets)
            {
                int x = details.BlockPosition.X + offset[0];
                int y = details.BlockPosition.Y;
                int z = details.BlockPosition.Z + offset[1];

                BlockPermutation neighborPermutation = dimension.GetPermutation(x, y, z);
                if (neighborPermutation.Type.Identifier != Block.Type.Identifier)
                {
                    continue;
                }

                Basalt.Block.Block? neighborBlock = dimension.GetBlock(x, y, z);
                ChestTrait? neighborChest = neighborBlock?.GetTrait<ChestTrait>();
                if (neighborChest is null || neighborChest.IsPaired)
                {
                    continue;
                }

                PairWith(
                    neighborChest,
                    details.BlockPosition.X,
                    details.BlockPosition.Z,
                    x,
                    z);

                CheckPairing(
                    dimension,
                    details.BlockPosition.X,
                    details.BlockPosition.Y,
                    details.BlockPosition.Z);

                neighborChest.CheckPairing(dimension, x, y, z);
                WriteStorage(dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
                neighborChest.WriteStorage(dimension, x, y, z);
                break;
            }
        }

        Container?.Show(details.Player);
    }

    public override void OnBreak(BlockBreakDetails details)
    {
        if (_container is not null)
        {
            foreach ((Basalt.Core.Player player, _) in _container.GetAllOccupants().ToList())
            {
                _container.Close(player);
            }
        }

        Unpair(details.Player.Dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
    }

    public override void OnRender(Core.Player player, int x, int y, int z)
    {
        if (!IsPaired || player.Dimension is null)
        {
            return;
        }

        CompoundTag nbt = new();

        nbt.Set("id", new StringTag { Value = "Chest" });
        nbt.Set("x", new IntTag { Value = x });
        nbt.Set("y", new IntTag { Value = y });
        nbt.Set("z", new IntTag { Value = z });

        OnWrite(nbt);

        player.Send(new BlockActorDataPacket
        {
            Position = new BlockPos { X = x, Y = y, Z = z },
            Data = nbt
        });

        BlockPermutation permutation = player.Dimension.GetPermutation(x, y, z);

        player.Send(CreateBlockUpdate(x, y, z, permutation.NetworkId));
    }

    public void CheckPairing(World.Dimension.Dimension? dimension, int x, int y, int z)
    {
        if (dimension is null)
        {
            return;
        }

        if (!IsPaired)
        {
            _sharedContainer = null;
            return;
        }

        int dx = Math.Abs(x - _pairX!.Value);
        int dz = Math.Abs(z - _pairZ!.Value);

        if (!((dx == 1 && dz == 0) || (dx == 0 && dz == 1)))
        {
            _pairX = null;
            _pairZ = null;
            _sharedContainer = null;
            return;
        }

        ChestTrait? pair = GetPair(dimension, y);
        if (pair is null)
        {
            _sharedContainer = null;
            return;
        }

        if (!pair.IsPaired || pair._pairX != x || pair._pairZ != z)
        {
            pair._pairX = x;
            pair._pairZ = z;
        }

        if (_sharedContainer is not null)
        {
            return;
        }

        if (pair._sharedContainer is not null)
        {
            _sharedContainer = pair._sharedContainer;
            return;
        }

        if (_container is null || pair._container is null)
        {
            return;
        }

        bool thisIsLeft = GetChestOrder(x, z) < GetChestOrder(_pairX.Value, _pairZ.Value);

        BlockContainer left = thisIsLeft ? _container : pair._container;
        BlockContainer right = thisIsLeft ? pair._container : _container;

        _sharedContainer = new BlockContainer(dimension, new BlockPos { X = x, Y = y, Z = z }, ContainerType.Container, 54);

        for (int slot = 0; slot < 27 && slot < left.GetSize(); slot++)
        {
            ItemStack? item = left.GetItem(slot);
            if (item is not null)
            {
                _sharedContainer.SetItem(slot, item);
            }
        }

        for (int slot = 0; slot < 27 && slot < right.GetSize(); slot++)
        {
            ItemStack? item = right.GetItem(slot);
            if (item is not null)
            {
                _sharedContainer.SetItem(slot + 27, item);
            }
        }

        pair._sharedContainer = _sharedContainer;
    }

    private void PairWith(ChestTrait other, int thisX, int thisZ, int otherX, int otherZ)
    {
        bool thisIsLead = GetChestOrder(thisX, thisZ) < GetChestOrder(otherX, otherZ);

        _pairX = otherX;
        _pairZ = otherZ;
        _isPairLead = thisIsLead;

        other._pairX = thisX;
        other._pairZ = thisZ;
        other._isPairLead = !thisIsLead;
    }

    private ChestTrait? GetPair(World.Dimension.Dimension dimension, int y)
    {
        if (!IsPaired)
        {
            return null;
        }

        Basalt.Block.Block? block = dimension.GetBlock(_pairX!.Value, y, _pairZ!.Value);
        return block?.GetTrait<ChestTrait>();
    }

    private void Unpair(World.Dimension.Dimension? dimension, int x, int y, int z)
    {
        if (!IsPaired || dimension is null)
        {
            return;
        }

        int? pairX = _pairX;
        int? pairZ = _pairZ;
        ChestTrait? pair = GetPair(dimension, y);

        if (_sharedContainer is not null &&
            _container is not null &&
            pair?._container is not null)
        {
            bool thisIsLeft = GetChestOrder(_container.Position.X, _container.Position.Z) <
                              GetChestOrder(pair._container.Position.X, pair._container.Position.Z);

            _container.Clear();
            pair._container.Clear();

            for (int slot = 0; slot < 27; slot++)
            {
                ItemStack? item = _sharedContainer.GetItem(slot);
                if (item is null)
                {
                    continue;
                }

                if (thisIsLeft)
                {
                    _container.SetItem(slot, item);
                }
                else
                {
                    pair._container.SetItem(slot, item);
                }
            }

            for (int slot = 27; slot < 54; slot++)
            {
                ItemStack? item = _sharedContainer.GetItem(slot);
                if (item is null)
                {
                    continue;
                }

                if (thisIsLeft)
                {
                    pair._container.SetItem(slot - 27, item);
                }
                else
                {
                    _container.SetItem(slot - 27, item);
                }
            }
        }

        _pairX = null;
        _pairZ = null;
        _sharedContainer = null;

        if (pair is null)
        {
            return;
        }

        pair._pairX = null;
        pair._pairZ = null;
        pair._sharedContainer = null;

        pair.CheckPairing(
            dimension,
            pair._container?.Position.X ?? 0,
            y,
            pair._container?.Position.Z ?? 0);

        WriteStorage(dimension, x, y, z);

        if (pairX.HasValue && pairZ.HasValue)
        {
            pair?.WriteStorage(dimension, pairX.Value, y, pairZ.Value);
        }
    }

    private void CopySharedItemsBackToSingleContainer()
    {
        if (_sharedContainer is null || _container is null || !IsPaired)
        {
            return;
        }

        bool thisIsLeft = GetChestOrder(_container.Position.X, _container.Position.Z) <
                          GetChestOrder(_pairX!.Value, _pairZ!.Value);

        int startSlot = thisIsLeft ? 0 : 27;

        _container.Clear();

        for (int slot = 0; slot < 27; slot++)
        {
            ItemStack? item = _sharedContainer.GetItem(startSlot + slot);

            if (item is not null)
            {
                _container.SetItem(slot, item);
            }
        }
    }

    private void EnsureContainer(World.Dimension.Dimension? dimension, int x, int y, int z)
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
    }

    private static UpdateBlockPacket CreateBlockUpdate(int x, int y, int z, int networkId)
    {
        return new UpdateBlockPacket
        {
            Position = new BlockPos { X = x, Y = y, Z = z },
            NetworkBlockId = (uint)networkId,
            Flags = Protocol.Enums.UpdateBlockFlagsType.Neighbors | Protocol.Enums.UpdateBlockFlagsType.Network,
            Layer = Protocol.Enums.UpdateBlockLayerType.Normal
        };
    }

    private static int GetChestOrder(int x, int z)
    {
        return x + (z << 15);
    }

    private void WriteStorage(World.Dimension.Dimension dimension, int x, int y, int z)
    {
        var chunk = dimension.GetOrCreateChunk(x >> 4, z >> 4);
        BlockPos position = new() { X = x, Y = y, Z = z };
        BlockLevelStorage? storage = chunk.GetBlockStorage(position);

        if (storage is null)
        {
            storage = new BlockLevelStorage(chunk);
            storage.SetPosition(position);
            storage.Set("id", new StringTag { Value = "Chest" });
            storage.Set("isMovable", new ByteTag { Value = 1 });
        }

        OnWrite(storage);
        chunk.SetBlockStorage(position, storage, dirty: true);
    }
}
