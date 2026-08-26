namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Enums;
using Basalt.Core.Item;
using Basalt.Core.Worlds;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public class ChestTrait : BlockTrait {
    public override bool Interactable => true;
    public static new readonly string Identifier = "minecraft:chest";
    public static new readonly string[] Types = ["minecraft:chest", "minecraft:trapped_chest"];

    private BlockContainer? _container;
    private BlockContainer? _sharedContainer;

    private int? _pairX;
    private int? _pairZ;
    private bool _isPairLead;

    public ChestTrait(Block block) : base(block) {
    }

    public bool IsPaired => _pairX.HasValue && _pairZ.HasValue;
    public BlockContainer? Container => _sharedContainer ?? _container;

    public override void OnRead(CompoundTag tag) {
        IntTag? pairX =
            tag.Get<IntTag>("pairx") ??
            tag.Get<IntTag>("pairX");
        IntTag? pairZ =
            tag.Get<IntTag>("pairz") ??
            tag.Get<IntTag>("pairZ");

        if (pairX is not null && pairZ is not null) {
            _pairX = pairX.Value;
            _pairZ = pairZ.Value;
        }
        else {
            _pairX = null;
            _pairZ = null;
        }

        if (tag.Get<IntTag>("pairlead") is { } pairLeadInt) {
            _isPairLead = pairLeadInt.Value == 1;
        }
        else if (tag.Get<ByteTag>("pairlead") is { } pairLeadByte) {
            _isPairLead = pairLeadByte.Value == 1;
        }
        else {
            _isPairLead = false;
        }

        if (tag.Get<ListTag>("Items") is not { } items) {
            return;
        }

        EnsureContainer(null, 0, 0, 0);

        foreach (BaseTag tagItem in items.Values) {
            if (tagItem is not CompoundTag itemTag) {
                continue;
            }

            int slot = itemTag.Get<ByteTag>("Slot") is { } slotTag
                ? (byte)slotTag.Value
                : 0;

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
        if (_pairX.HasValue) {
            tag.Set("pairx", new IntTag { Value = _pairX.Value });
        }

        if (_pairZ.HasValue) {
            tag.Set("pairz", new IntTag { Value = _pairZ.Value });
        }

        tag.Set("pairlead", new IntTag { Value = _isPairLead ? 1 : 0 });

        if (_sharedContainer is not null && _container is not null && IsPaired) {
            CopySharedItemsBackToSingleContainer();
        }

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

    public override void OnPlace(BlockPlaceDetails details) {
        var dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        EnsureContainer(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);

        List<(int X, int Z, ChestTrait Chest)> candidates = [];
        int[][] offsets = GetPairOffsets();

        for (int i = 0; i < offsets.Length; i++) {
            int x = details.BlockPosition.X + offsets[i][0];
            int z = details.BlockPosition.Z + offsets[i][1];
            int y = details.BlockPosition.Y;

            BlockPermutation neighborPermutation = dimension.GetLoadedPermutationOrAir(x, y, z);
            if (neighborPermutation.Type.Identifier != Block.Type.Identifier) {
                continue;
            }

            Basalt.Core.Blocks.Block? neighborBlock = dimension.GetBlock(x, y, z);
            ChestTrait? neighborChest = neighborBlock?.GetTrait<ChestTrait>();
            if (neighborChest is null || neighborChest.IsPaired || !CanPairWith(neighborChest)) {
                continue;
            }

            candidates.Add((x, z, neighborChest));
        }

        if (candidates.Count != 1) {
            return;
        }

        (int pairX, int pairZ, ChestTrait pairChest) = candidates[0];

        AlignFacingWith(pairChest);

        PairWith(
            pairChest,
            details.BlockPosition.X,
            details.BlockPosition.Z,
            pairX,
            pairZ);

        CheckPairing(
            dimension,
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z);

        pairChest.CheckPairing(dimension, pairX, details.BlockPosition.Y, pairZ);
        WriteStorage(dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
        pairChest.WriteStorage(dimension, pairX, details.BlockPosition.Y, pairZ);

    }

    public override void OnInteract(BlockInteractDetails details) {
        var dimension = details.Player.Dimension;
        if (dimension is null) {
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

        Container?.Show(details.Player);
    }

    public override void OnBreak(BlockBreakDetails details) {
        if (_container is not null) {
            foreach ((Basalt.Core.Player.Player player, _) in _container.GetAllOccupants().ToList()) {
                _container.Close(player);
            }
        }

        if (_sharedContainer is not null) {
            foreach ((Basalt.Core.Player.Player player, _) in _sharedContainer.GetAllOccupants().ToList()) {
                _sharedContainer.Close(player);
            }
        }

        DropContainerItems(details);
        Unpair(details.Player.Dimension, details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z);
    }

    private void DropContainerItems(BlockBreakDetails details) {
        var dimension = details.Player.Dimension;
        if (dimension is null || _container is null) return;

        ulong currentTick = dimension.World is Worlds.Tickable tickable ? tickable.TickValue : 0;

        for (int i = 0; i < _container.GetSize(); i++) {
            ItemStack? item = _container.GetItem(i);
            if (item is null || item.StackSize == 0) continue;

            Entities.ItemEntity drop = new(item) {
                Position = new Vec3 {
                    X = details.BlockPosition.X + 0.5f,
                    Y = details.BlockPosition.Y + 0.5f,
                    Z = details.BlockPosition.Z + 0.5f
                }
            };

            drop.LockPickupUntil(currentTick + 10);
            drop.Spawn(dimension, new Entities.Traits.Types.EntitySpawnOptions(InitialSpawn: false));
        }
    }

    public override void OnRender(Player.Player player, int x, int y, int z) {
        var dimension = player.Dimension;
        if (dimension is null) {
            return;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };

        EnsureContainer(dimension, x, y, z);
        CheckPairing(dimension, x, y, z);
        WriteStorage(dimension, x, y, z);

        BlockLevelStorage? storage = dimension
            .GetLoadedChunk(x >> 4, z >> 4)
            ?.GetBlockStorage(position);

        if (storage is null) {
            return;
        }

        uint networkId = (uint)dimension.GetLoadedPermutationOrAir(x, y, z).NetworkId;

        player.Send(
            new BlockActorDataPacket {
                Position = position,
                ActorData = storage,
                
            },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = 0,
                Flags = (uint)UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = networkId,
                Flags = (uint)UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            }
        );
    }

    public void CheckPairing(global::Basalt.Core.Worlds.Dimensions.Dimension? dimension, int x, int y, int z) {
        if (dimension is null) {
            return;
        }

        bool wasPaired = IsPaired;

        if (!IsPaired) {
            ChestTrait? discoveredPair = FindUnpairedNeighbor(dimension, x, y, z, out int discoveredX, out int discoveredZ);
            if (discoveredPair is null) {
                _sharedContainer = null;
                return;
            }

            PairWith(discoveredPair, x, z, discoveredX, discoveredZ);
            CheckPairing(dimension, x, y, z);
            discoveredPair.CheckPairing(dimension, discoveredX, y, discoveredZ);
            return;
        }

        if (!IsValidPairOffset(x, z, _pairX!.Value, _pairZ!.Value)) {
            _pairX = null;
            _pairZ = null;
            _sharedContainer = null;
            return;
        }

        if (dimension.GetLoadedChunk(_pairX.Value >> 4, _pairZ.Value >> 4) is null) {
            _sharedContainer = null;
            return;
        }

        ChestTrait? pair = GetPair(dimension, y);
        if (pair is null) {
            _pairX = null;
            _pairZ = null;
            _sharedContainer = null;
            return;
        }

        if (!CanPairWith(pair)) {
            bool wasSavedPair =
                pair.IsPaired &&
                pair._pairX == x &&
                pair._pairZ == z;

            if (!wasSavedPair) {
                _pairX = null;
                _pairZ = null;
                _sharedContainer = null;
                pair._pairX = null;
                pair._pairZ = null;
                pair._sharedContainer = null;
                return;
            }
        }

        bool validPairPosition = false;
        foreach (int[] offset in GetPairOffsets()) {
            if (x + offset[0] == _pairX!.Value && z + offset[1] == _pairZ!.Value) {
                validPairPosition = true;
                break;
            }
        }

        if (!validPairPosition) {
            _pairX = null;
            _pairZ = null;
            _sharedContainer = null;
            pair._pairX = null;
            pair._pairZ = null;
            pair._sharedContainer = null;
            return;
        }

        if (!pair.IsPaired || pair._pairX != x || pair._pairZ != z) {
            pair._pairX = x;
            pair._pairZ = z;
        }

        bool thisIsLead = ShouldBePairLead(x, z, _pairX.Value, _pairZ.Value);
        _isPairLead = thisIsLead;
        pair._isPairLead = !thisIsLead;

        if (_sharedContainer is not null) {
            return;
        }

        if (pair._sharedContainer is not null) {
            _sharedContainer = pair._sharedContainer;
            return;
        }

        EnsureContainer(dimension, x, y, z);
        pair.EnsureContainer(dimension, _pairX.Value, y, _pairZ.Value);

        if (_container is null || pair._container is null) {
            return;
        }

        bool thisIsLeft = GetChestOrder(x, z) < GetChestOrder(_pairX.Value, _pairZ.Value);

        BlockContainer left = thisIsLeft ? _container : pair._container;
        BlockContainer right = thisIsLeft ? pair._container : _container;

        _sharedContainer = new BlockContainer(dimension, new BlockPos { X = x, Y = y, Z = z }, ContainerType.CONTAINER, 54);
        _sharedContainer.OnViewerAddedEvent = OnViewerAdded;
        _sharedContainer.OnViewerRemovedEvent = OnViewerRemoved;
        _sharedContainer.OnContainerUpdated = OnContainerUpdated;

        for (int slot = 0; slot < 27 && slot < left.GetSize(); slot++) {
            ItemStack? item = left.GetItem(slot);
            if (item is not null) {
                _sharedContainer.SetItem(slot, item);
            }
        }

        for (int slot = 0; slot < 27 && slot < right.GetSize(); slot++) {
            ItemStack? item = right.GetItem(slot);
            if (item is not null) {
                _sharedContainer.SetItem(slot + 27, item);
            }
        }

        pair._sharedContainer = _sharedContainer;

        _ = wasPaired;
    }

    private void PairWith(ChestTrait other, int thisX, int thisZ, int otherX, int otherZ) {
        bool thisIsLead = ShouldBePairLead(thisX, thisZ, otherX, otherZ);

        _pairX = otherX;
        _pairZ = otherZ;
        _isPairLead = thisIsLead;

        other._pairX = thisX;
        other._pairZ = thisZ;
        other._isPairLead = !thisIsLead;
    }

    private ChestTrait? GetPair(global::Basalt.Core.Worlds.Dimensions.Dimension dimension, int y) {
        if (!IsPaired) {
            return null;
        }

        Basalt.Core.Blocks.Block? block = dimension.GetBlock(_pairX!.Value, y, _pairZ!.Value);
        return block?.GetTrait<ChestTrait>();
    }

    private void Unpair(global::Basalt.Core.Worlds.Dimensions.Dimension? dimension, int x, int y, int z) {
        if (!IsPaired || dimension is null) {
            return;
        }

        int? pairX = _pairX;
        int? pairZ = _pairZ;
        ChestTrait? pair = GetPair(dimension, y);

        if (_sharedContainer is not null &&
            _container is not null &&
            pair?._container is not null) {
            bool thisIsLeft = GetChestOrder(_container.Position.X, _container.Position.Z) <
                              GetChestOrder(pair._container.Position.X, pair._container.Position.Z);

            _container.Clear();
            pair._container.Clear();

            for (int slot = 0; slot < 27; slot++) {
                ItemStack? item = _sharedContainer.GetItem(slot);
                if (item is null) {
                    continue;
                }

                if (thisIsLeft) {
                    _container.SetItem(slot, item);
                }
                else {
                    pair._container.SetItem(slot, item);
                }
            }

            for (int slot = 27; slot < 54; slot++) {
                ItemStack? item = _sharedContainer.GetItem(slot);
                if (item is null) {
                    continue;
                }

                if (thisIsLeft) {
                    pair._container.SetItem(slot - 27, item);
                }
                else {
                    _container.SetItem(slot - 27, item);
                }
            }
        }

        _pairX = null;
        _pairZ = null;
        _sharedContainer = null;

        if (pair is null) {
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

        if (pairX.HasValue && pairZ.HasValue) {
            pair?.WriteStorage(dimension, pairX.Value, y, pairZ.Value);
        }
    }

    private void CopySharedItemsBackToSingleContainer() {
        if (_sharedContainer is null || _container is null || !IsPaired) {
            return;
        }

        bool thisIsLeft = GetChestOrder(_container.Position.X, _container.Position.Z) <
                          GetChestOrder(_pairX!.Value, _pairZ!.Value);

        int startSlot = thisIsLeft ? 0 : 27;

        _container.Clear();

        for (int slot = 0; slot < 27; slot++) {
            ItemStack? item = _sharedContainer.GetItem(startSlot + slot);

            if (item is not null) {
                _container.SetItem(slot, item);
            }
        }
    }

    private void EnsureContainer(global::Basalt.Core.Worlds.Dimensions.Dimension? dimension, int x, int y, int z) {
        if (_container is not null) {
            if (dimension is not null && _container.Dimension is null) {
                _container.Dimension = dimension;
                _container.Position = new BlockPos { X = x, Y = y, Z = z };
            }

            return;
        }

        _container = new BlockContainer(dimension, new BlockPos { X = x, Y = y, Z = z }, ContainerType.CONTAINER, 27);
        _container.OnViewerAddedEvent = OnViewerAdded;
        _container.OnViewerRemovedEvent = OnViewerRemoved;
        _container.OnContainerUpdated = OnContainerUpdated;
    }

    private void OnContainerUpdated(BlockContainer container) {
        if (container.Dimension is null) {
            return;
        }

        var chunk = container.Dimension.GetLoadedChunk(container.Position.X >> 4, container.Position.Z >> 4);
        if (chunk is not null) {
            chunk.Dirty = true;
        }
    }

    private static int GetItemCount(BlockContainer? container) {
        if (container is null) {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < container.GetSize(); i++) {
            ItemStack? item = container.GetItem(i);
            if (item is not null && item.StackSize > 0) {
                count++;
            }
        }

        return count;
    }

    public int DebugSingleContainerItemCount() {
        return GetItemCount(_container);
    }

    public int DebugSharedContainerItemCount() {
        return GetItemCount(_sharedContainer);
    }

    public bool GetPairCoordinates(out int pairX, out int pairZ) {
        if (!IsPaired || !_pairX.HasValue || !_pairZ.HasValue) {
            pairX = 0;
            pairZ = 0;
            return false;
        }

        pairX = _pairX.Value;
        pairZ = _pairZ.Value;
        return true;
    }

    public bool GetPairData(out int pairX, out int pairZ, out bool pairLead) {
        bool paired = GetPairCoordinates(out pairX, out pairZ);
        pairLead = paired && _isPairLead;
        return paired;
    }

    private void OnViewerAdded(BlockContainer container, Basalt.Core.Player.Player _) {
        if (container.occupants.Count != 1) {
            return;
        }

        BroadcastState(1, "chest.open", container.Position);

        if (GetPairPosition(container.Position.Y, out BlockPos pairPosition)) {
            BroadcastState(1, "chest.open", pairPosition);
        }
    }

    private void OnViewerRemoved(BlockContainer container, Basalt.Core.Player.Player _) {
        if (container.occupants.Count != 0) {
            return;
        }

        BroadcastState(0, "chest.closed", container.Position);

        if (GetPairPosition(container.Position.Y, out BlockPos pairPosition)) {
            BroadcastState(0, "chest.closed", pairPosition);
        }
    }

    private bool GetPairPosition(int y, out BlockPos position) {
        position = new BlockPos();

        if (!IsPaired || Container?.Dimension is null) {
            return false;
        }

        ChestTrait? pair = GetPair(Container.Dimension, y);
        if (pair is null || !pair.IsPaired) {
            return false;
        }

        int pairX = pair._container?.Position.X ?? pair._pairX ?? 0;
        int pairZ = pair._container?.Position.Z ?? pair._pairZ ?? 0;

        position = new BlockPos {
            X = pairX,
            Y = y,
            Z = pairZ
        };

        return true;
    }

    private void BroadcastState(int state, string soundEvent, BlockPos position) {
        if (Container?.Dimension is null) {
            return;
        }

        var chunk = Container.Dimension.GetLoadedChunk(position.X >> 4, position.Z >> 4);
        BlockLevelStorage? storage = chunk?.GetBlockStorage(position);
        if (storage is null) {
            WriteStorage(Container.Dimension, position.X, position.Y, position.Z);
            storage = chunk?.GetBlockStorage(position);
        }

        if (storage is not null) {
            Container.Dimension.Broadcast(new BlockActorDataPacket {
                Position = position,
                ActorData = storage
            });
        }

        Logger.Warn(
            "Chest block event position:{0},{1},{2} type:{3} value:{4}",
            position.X,
            position.Y,
            position.Z,
            BlockEventType.ChangeState,
            state);

        Container.Dimension.Broadcast(new BlockEventPacket {
            Position = position,
            EventType = (int)BlockEventType.ChangeState,
            EventValue = state
        });

        int runtimeId = Container.Dimension.GetLoadedPermutationOrAir(position.X, position.Y, position.Z).NetworkId;

        Container.Dimension.PlaySound(soundEvent, new Vec3 {
                X = position.X + 0.5f,
                Y = position.Y + 0.5f,
                Z = position.Z + 0.5f
            },
            data: runtimeId,
            uniqueActorId: -1);
    }

    private static int GetChestOrder(int x, int z) {
        return x + (z << 15);
    }

    private static BlockPos GetClickedBlockPosition(BlockPos placedPosition, int face) {
        return face switch {
            0 => new BlockPos { X = placedPosition.X, Y = placedPosition.Y + 1, Z = placedPosition.Z },
            1 => new BlockPos { X = placedPosition.X, Y = placedPosition.Y - 1, Z = placedPosition.Z },
            2 => new BlockPos { X = placedPosition.X, Y = placedPosition.Y, Z = placedPosition.Z + 1 },
            3 => new BlockPos { X = placedPosition.X, Y = placedPosition.Y, Z = placedPosition.Z - 1 },
            4 => new BlockPos { X = placedPosition.X + 1, Y = placedPosition.Y, Z = placedPosition.Z },
            5 => new BlockPos { X = placedPosition.X - 1, Y = placedPosition.Y, Z = placedPosition.Z },
            _ => placedPosition
        };
    }

    private bool ShouldBePairLead(int thisX, int thisZ, int otherX, int otherZ) {
        if (!GetCardinalFacing(out string direction)) {
            return GetChestOrder(thisX, thisZ) < GetChestOrder(otherX, otherZ);
        }

        bool axisX = direction is "north" or "south";
        int current = axisX ? thisX : thisZ;
        int other = axisX ? otherX : otherZ;

        return direction is "north" or "east"
            ? current > other
            : current < other;
    }

    private int[][] GetPairOffsets() {
        if (!GetCardinalFacing(out string direction)) {
            return [[1, 0], [-1, 0], [0, 1], [0, -1]];
        }

        if (direction is "north" or "south") {
            return [[1, 0], [-1, 0]];
        }

        if (direction is "east" or "west") {
            return [[0, 1], [0, -1]];
        }

        return [[1, 0], [-1, 0], [0, 1], [0, -1]];
    }

    private static bool IsValidPairOffset(int x, int z, int pairX, int pairZ) {
        int deltaX = Math.Abs(pairX - x);
        int deltaZ = Math.Abs(pairZ - z);
        return deltaX + deltaZ == 1;
    }

    private bool CanPairWith(ChestTrait other) {
        if (!GetFacingValue(Block.Permutation.State, out string thisFacing)) {
            return true;
        }

        if (!GetFacingValue(other.Block.Permutation.State, out string otherFacing)) {
            return true;
        }

        return string.Equals(thisFacing, otherFacing, StringComparison.Ordinal);
    }

    private void AlignFacingWith(ChestTrait other) {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        if (GetValue(other.Block.Permutation.State, "minecraft:cardinal_direction", out BlockStateValue cardinal)) {
            state["minecraft:cardinal_direction"] = cardinal;
        }
        else if (GetValue(other.Block.Permutation.State, "facing_direction", out BlockStateValue facing)) {
            state["facing_direction"] = facing;
        }
        else {
            return;
        }

        Block.SetPermutation(Block.Type.GetPermutation(state));
    }

    private static bool GetFacingValue(BlockState state, out string facing) {
        if (GetValue(state, "minecraft:cardinal_direction", out BlockStateValue cardinal) && cardinal.Kind == 1) {
            facing = $"cardinal:{cardinal.AsString()}";
            return true;
        }

        if (GetValue(state, "facing_direction", out BlockStateValue direction) && direction.Kind == 0) {
            facing = $"facing:{direction.AsNumber()}";
            return true;
        }

        facing = string.Empty;
        return false;
    }

    private bool GetCardinalFacing(out string direction) {
        if (!GetFacingValue(Block.Permutation.State, out string facing)) {
            direction = string.Empty;
            return false;
        }

        if (facing.StartsWith("cardinal:", StringComparison.Ordinal)) {
            direction = facing["cardinal:".Length..];
            return true;
        }

        if (!facing.StartsWith("facing:", StringComparison.Ordinal)) {
            direction = string.Empty;
            return false;
        }

        string raw = facing["facing:".Length..];
        direction = raw switch {
            "2" => "north",
            "3" => "south",
            "4" => "west",
            "5" => "east",
            _ => string.Empty
        };

        return direction.Length != 0;
    }

    private void WriteStorage(global::Basalt.Core.Worlds.Dimensions.Dimension dimension, int x, int y, int z) {
        var chunk = dimension.GetOrCreateChunk(x >> 4, z >> 4);
        BlockPos position = new() { X = x, Y = y, Z = z };
        BlockLevelStorage? storage = chunk.GetBlockStorage(position);

        if (storage is null) {
            storage = new BlockLevelStorage(chunk);
            storage.SetPosition(position);
            storage.Set("id", new StringTag { Value = "Chest" });
            storage.Set("isMovable", new ByteTag { Value = 1 });
        }

        storage.Delete("pairx");
        storage.Delete("pairz");
        storage.Delete("pairlead");

        OnWrite(storage);
        chunk.SetBlockStorage(position, storage, dirty: true);

        _ = dimension;
    }

    private ChestTrait? FindUnpairedNeighbor(global::Basalt.Core.Worlds.Dimensions.Dimension dimension, int x, int y, int z, out int pairX, out int pairZ) {
        pairX = 0;
        pairZ = 0;

        List<(int X, int Z, ChestTrait Chest)> candidates = [];
        int[][] offsets = GetPairOffsets();

        for (int i = 0; i < offsets.Length; i++) {
            int candidateX = x + offsets[i][0];
            int candidateZ = z + offsets[i][1];

            Basalt.Core.Blocks.Block? neighborBlock = dimension.GetBlock(candidateX, y, candidateZ);
            ChestTrait? neighborChest = neighborBlock?.GetTrait<ChestTrait>();
            if (neighborChest is null || neighborChest.IsPaired || !CanPairWith(neighborChest)) {
                continue;
            }

            candidates.Add((candidateX, candidateZ, neighborChest));
        }

        if (candidates.Count != 1) {
            return null;
        }

        pairX = candidates[0].X;
        pairZ = candidates[0].Z;
        return candidates[0].Chest;
    }

    private static bool GetValue(BlockState state, string key, out BlockStateValue value) {
        return state.TryGetValue(key, out value);
    }

}
