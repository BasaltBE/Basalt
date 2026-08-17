namespace Basalt.Core.Worlds.Dimensions;

using System.Collections.Concurrent;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities;
using Basalt.Core.Item;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.Core.Player.Traits;
using Basalt.Core.Pathfinding;
using ChunkColumn = Chunk.Chunk;

using Entity = Entities.Entity;

using BedrockProtocol.Types;
using BedrockProtocol.Packets;
using BedrockProtocol.Enums;
using BedrockProtocol.Nbt;

public sealed class Dimension : IDisposable {
    private const int CompletedChunkLimit = 128;
    private const float VoidY = -64f;
    private const ulong VoidDamageCooldownTicks = 20;

    private static readonly Dictionary<string, string> BlockActorIds = new() {
        ["minecraft:barrel"] = "Barrel",
        ["minecraft:chest"] = "Chest",
        ["minecraft:trapped_chest"] = "Chest",
        ["minecraft:hopper"] = "Hopper",
        ["minecraft:furnace"] = "Furnace",
        ["minecraft:lit_furnace"] = "Furnace",
        ["minecraft:blast_furnace"] = "BlastFurnace",
        ["minecraft:lit_blast_furnace"] = "BlastFurnace",
        ["minecraft:smoker"] = "Smoker",
        ["minecraft:lit_smoker"] = "Smoker",
        ["minecraft:mob_spawner"] = "MobSpawner"
    };

    private readonly Dictionary<long, ChunkColumn> _chunks;
    private readonly object _chunkAccessLock = new();
    private readonly Dictionary<long, int> _chunkViewers;
    private readonly HashSet<long> _pendingUnloads = [];
    private readonly HashSet<Entity> _entities;
    private readonly Dictionary<Entity, long> _entityChunks = [];
    private readonly Dictionary<Entity, long> _entityChunkIndexes = [];
    private readonly Dictionary<long, HashSet<Entity>> _chunkEntities = [];
    private readonly List<Entity> _tickEntityBuffer = [];
    private readonly List<long> _chunkSweepBuffer = [];
    private readonly HashSet<Entity> _pendingEntityAdds = [];
    private readonly HashSet<Entity> _pendingEntityRemoves = [];
    private readonly Dictionary<Player.Player, long> _simulationPlayerChunks = [];
    private readonly HashSet<Player.Player> _simulationPlayerBuffer = [];
    private readonly List<Player.Player> _simulationPlayerRemovalBuffer = [];
    private readonly HashSet<long> _simulatedChunks = [];
    private readonly HashSet<long> _simulationChunkBuffer = [];
    private readonly Dictionary<(int X, int Y, int Z), BlockTickTask> _blockTicks = [];
    private readonly Lock _chunkRequestLock = new();
    private readonly Dictionary<long, PendingChunkRequest> _pendingChunkRequests = [];
    private readonly ConcurrentQueue<ChunkRequestCallback> _chunkRequestCallbacks = new();
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private Vec3 _spawnPosition = new() {
        X = 0,
        Y = 80,
        Z = 0,
    };
    private ChunkColumn[]? _autoSaveChunks;
    private int _autoSaveIndex;
    private int _simulationDistance = -1;
    private bool _tickingEntities;
    private bool _disposed;

    public string Identifier { get; }
    public DimensionId Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public Vec3 SpawnPosition {
        get => _spawnPosition;
        set {
            _spawnPosition = value;
            if (World is not null) {
                GetOrCreateChunk(WorldToChunk(value.X), WorldToChunk(value.Z));
            }
        }
    }
    public World? World {
        get => _world;
        internal set {
            _world = value;
            if (value is not null) {
                GetOrCreateChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z));
            }
        }
    }
    private World? _world;
    public DimensionGameRules Gamerules { get; } = new();

    public bool IsDay() {
        int time = World?.CurrentDayTime ?? 0;
        return time < 12000;
    }

    public bool IsNight() {
        return !IsDay();
    }

    public Dimension(string identifier, DimensionId type, WorldProvider provider, Generator? generator = null) {
        Identifier = identifier;
        Type = type;
        _chunks = [];
        _chunkViewers = [];
        _entities = [];
        _provider = provider;
        _generator = generator ?? new VoidGenerator();
    }

    public int ChunkCount => _chunks.Count;
    public int ChunkViewerCount => _chunkViewers.Count;
    public int PendingChunkRequestCount {
        get {
            lock (_chunkRequestLock) {
                return _pendingChunkRequests.Count;
            }
        }
    }
    public int PendingChunkCallbackCount => _chunkRequestCallbacks.Count;
    public IReadOnlyCollection<Entity> Entities => _entities;

    internal IReadOnlyCollection<Entity> GetEntities(int x, int z) {
        return _chunkEntities.TryGetValue(HashChunk(x, z), out HashSet<Entity>? entities)
            ? entities
            : Array.Empty<Entity>();
    }

    internal bool ChunkLoaded(int x, int z) {
        return _chunks.ContainsKey(HashChunk(x, z));
    }

    public bool HasChunk(int x, int z) {
        long hash = HashChunk(x, z);
        return _chunks.ContainsKey(hash) ||
            World?.Persistence.ChunkPending(Type, x, z) == true ||
            _provider.HasChunk(Type, x, z);
    }

    public ChunkColumn? GetChunk(int x, int z) {
        return GetOrLoadChunk(x, z);
    }

    public ChunkColumn GetOrCreateChunk(int x, int z) {
        lock (_chunkAccessLock) {
            ChunkColumn? chunk = GetOrLoadChunk(x, z);
            if (chunk is not null) {
                return chunk;
            }

            long hash = HashChunk(x, z);

        if (_provider.HasChunk(Type, x, z)) {
            Logger.Warn($"Chunk {x},{z} exists in storage but failed to load; regenerating.");
        }
        else {
            // Logger.Warn($"Chunk {x},{z} in '{Identifier}' NOT in provider, generating fresh!");
        }

        chunk = _generator.Generate(Type, x, z);
        _generator.Populate(chunk);
        chunk.Dirty = true;
        chunk.Simulated = _simulatedChunks.Contains(hash);
        _chunks[hash] = chunk;
        if (chunk.Simulated) {
            RestoreBlockTicks(chunk);
        }
            return chunk;
        }
    }

    public void SetChunk(ChunkColumn chunk) {
        lock (_chunkAccessLock) {
            long hash = HashChunk(chunk.X, chunk.Z);
            World?.Persistence.WaitForChunk(Type, chunk.X, chunk.Z);
            chunk.Simulated = _simulatedChunks.Contains(hash);
            _chunks[hash] = chunk;
            MaterializeEntities(chunk);
            if (chunk.Simulated) {
                RestoreBlockTicks(chunk);
            }
            SyncEntitiesToStorage(chunk);
            _provider.SaveChunk(chunk);
        }
    }

    public void RequestChunks(ReadOnlySpan<(int X, int Z)> chunks, Action<ChunkColumn> ready) {
        if (_disposed) {
            return;
        }

        TaskScheduler? scheduler = World?.Server?.Scheduler;

        for (int i = 0; i < chunks.Length; i++) {
            (int x, int z) = chunks[i];
            long hash = HashChunk(x, z);

            if (_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                _chunkRequestCallbacks.Enqueue(new ChunkRequestCallback(chunk, ready));
                continue;
            }

            World?.Persistence.WaitForChunk(Type, x, z);

            lock (_chunkRequestLock) {
                if (_pendingChunkRequests.TryGetValue(hash, out PendingChunkRequest? request)) {
                    request.Callbacks.Add(ready);
                    continue;
                }

                _pendingChunkRequests[hash] = new PendingChunkRequest(ready);
                _chunkViewers[hash] = _chunkViewers.TryGetValue(hash, out int count) ? count + 1 : 1;
            }

            if (scheduler is null) {
                ChunkColumn? loaded = _provider.LoadChunk(Type, x, z);
                if (loaded is null) {
                    loaded = _generator.Generate(Type, x, z);
                    _generator.Populate(loaded);
                    loaded.Dirty = true;
                }
                HandleChunkCompleted(hash, loaded);
            }
            else {
                ChunkGenerationTask task = new(_provider, _generator, Type, x, z, hash, HandleChunkCompleted);
                scheduler.Schedule(task);
            }
        }
    }

    public bool RemoveChunk(int x, int z) {
        lock (_chunkAccessLock) {
            if (HashChunk(x, z) == HashChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z))) {
                return false;
            }

        World?.Persistence.WaitForChunk(Type, x, z);
        _provider.DeleteChunk(Type, x, z);
        long hash = HashChunk(x, z);
            return _chunks.Remove(hash);
        }
    }

    public void SaveDirtyChunks() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Dimension.SaveDirtyChunks") : default;

        _provider.SaveSpawnPosition(Type, SpawnPosition);

        foreach (ChunkColumn loadedChunk in _chunks.Values) {
            SyncBlockActorsToStorages(loadedChunk);
            SyncEntitiesToStorage(loadedChunk);
        }

        foreach (ChunkColumn chunk in _chunks.Values) {
            if (!chunk.Dirty) {
                continue;
            }

            try {
                _provider.SaveChunk(chunk);
                chunk.Dirty = false;
            }
            catch (Exception exception) {
                Logger.Err($"Failed to save chunk {chunk.X},{chunk.Z}: {exception.Message}");
            }
        }
    }

    public bool SaveChunk(int x, int z) {
        if (!_chunks.TryGetValue(HashChunk(x, z), out ChunkColumn? chunk)) {
            return false;
        }

        SyncBlockActorsToStorages(chunk);
        SyncEntitiesToStorage(chunk);
        try {
            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }
        catch (Exception exception) {
            Logger.Err($"Failed to save chunk {x},{z}: {exception.Message}");
            return false;
        }

        return true;
    }

    public bool UnloadChunk(int x, int z, bool save = true) {
        lock (_chunkAccessLock) {
            long hash = HashChunk(x, z);
        if (hash == HashChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z))) {
            return false;
        }

        if (!_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
            return false;
        }

        SyncEntitiesToStorage(chunk);
        if (save && chunk.Dirty) {
            SyncBlockActorsToStorages(chunk);
            chunk.Dirty = false;
            _chunks.Remove(hash);
            if (World is { } world) {
                world.Persistence.SaveChunk(chunk);
            }
            else {
                try {
                    _provider.SaveChunk(chunk);
                }
                catch (Exception exception) {
                    Logger.Err($"Failed to save chunk {x},{z} on unload: {exception.Message}");
                }
            }
            UnloadEntities(chunk);
            return true;
        }

        bool removed = _chunks.Remove(hash);
        if (removed) {
            UnloadEntities(chunk);
        }
            return removed;
        }
    }

    public void AddChunkViewer(int x, int z) {
        long hash = HashChunk(x, z);
        _chunkViewers[hash] = _chunkViewers.TryGetValue(hash, out int count) ? count + 1 : 1;
        _pendingUnloads.Remove(hash);
    }

    public bool RemoveChunkViewer(int x, int z) {
        long hash = HashChunk(x, z);
        if (!_chunkViewers.TryGetValue(hash, out int count)) {
            return false;
        }

        if (count <= 1) {
            _chunkViewers.Remove(hash);
            _pendingUnloads.Add(hash);
            return true;
        }

        _chunkViewers[hash] = count - 1;
        return true;
    }

    public bool HasChunkViewers(int x, int z) {
        return _chunkViewers.ContainsKey(HashChunk(x, z));
    }

    public int UnloadUnviewedChunks(int limit, bool save = true) {
        if (_chunks.Count == 0 || limit <= 0) {
            return 0;
        }

        int unloaded = 0;
        _chunkSweepBuffer.Clear();

        HashSet<long>? pendingSnapshot = null;
        lock (_chunkRequestLock) {
            if (_pendingChunkRequests.Count > 0) {
                pendingSnapshot = new HashSet<long>(_pendingChunkRequests.Keys);
            }
        }

        foreach (long hash in _chunks.Keys) {
            if (hash == HashChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z))) {
                continue;
            }

            if (_chunkViewers.ContainsKey(hash)) {
                continue;
            }

            if (pendingSnapshot is not null && pendingSnapshot.Contains(hash)) {
                continue;
            }

            _chunkSweepBuffer.Add(hash);
            if (_chunkSweepBuffer.Count >= limit) {
                break;
            }
        }

        for (int i = 0; i < _chunkSweepBuffer.Count; i++) {
            long hash = _chunkSweepBuffer[i];
            int x = (int)(hash >> 32);
            int z = (int)hash;
            if (UnloadChunk(x, z, save)) {
                unloaded++;
            }
        }

        return unloaded;
    }

    public IEnumerable<ChunkColumn> GetChunks() {
        return _chunks.Values;
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
    }

    public PathfindingSnapshot CreatePathfindingSnapshot(
        PathNode start,
        PathNode target,
        int radius = 32,
        int verticalRange = 8) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentOutOfRangeException.ThrowIfNegative(verticalRange);

        int minX = Math.Min(start.X, target.X) - radius;
        int maxX = Math.Max(start.X, target.X) + radius;
        int minY = Math.Min(start.Y, target.Y) - verticalRange;
        int maxY = Math.Max(start.Y, target.Y) + verticalRange;
        int minZ = Math.Min(start.Z, target.Z) - radius;
        int maxZ = Math.Max(start.Z, target.Z) + radius;
        int width = checked(maxX - minX + 1);
        int height = checked(maxY - minY + 1);
        int depth = checked(maxZ - minZ + 1);
        bool[] walkable = new bool[checked(width * height * depth)];

        for (int y = minY; y <= maxY; y++) {
            for (int z = minZ; z <= maxZ; z++) {
                for (int x = minX; x <= maxX; x++) {
                    if (!TryGetLoadedPermutation(x, y - 1, z, out BlockPermutation? below) ||
                        !TryGetLoadedPermutation(x, y, z, out BlockPermutation? feet) ||
                        !TryGetLoadedPermutation(x, y + 1, z, out BlockPermutation? head)) {
                        continue;
                    }

                    bool supported = HasFullHeightSupport(below!);
                    bool clear = BlockCollisionShape.GetBoxes(feet!).Count == 0 &&
                        BlockCollisionShape.GetBoxes(head!).Count == 0;
                    if (supported && clear) {
                        int index = ((y - minY) * depth + z - minZ) * width + x - minX;
                        walkable[index] = true;
                    }
                }
            }
        }

        return new PathfindingSnapshot(minX, minY, minZ, width, height, depth, walkable);
    }

    private static bool HasFullHeightSupport(BlockPermutation permutation) {
        foreach (CollisionBox box in BlockCollisionShape.GetBoxes(permutation)) {
            if (box.OriginY + box.SizeY >= 16f) {
                return true;
            }
        }

        return false;
    }

    public void RequestPath(
        PathNode start,
        PathNode target,
        Action<Path?> completion,
        int radius = 32,
        int verticalRange = 8,
        int maxVisitedNodes = 4096,
        float maxDistance = 32f) {
        ArgumentNullException.ThrowIfNull(completion);
        PathfindingSnapshot snapshot = CreatePathfindingSnapshot(start, target, radius, verticalRange);
        PathfindingTask task = new(snapshot, start, target, completion, maxVisitedNodes, maxDistance);

        if (World?.Scheduler is { } scheduler) {
            scheduler.Schedule(task);
        }
        else {
            task.Execute();
            task.Complete();
        }
    }

    public bool TryGetLoadedPermutation(int x, int y, int z, out BlockPermutation? permutation, int layer = 0) {
        lock (_chunkAccessLock) {
            if (!_chunks.TryGetValue(HashChunk(x >> 4, z >> 4), out ChunkColumn? chunk)) {
                permutation = null;
                return false;
            }

            permutation = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
            return true;
        }
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true, bool broadcast = true) {
        lock (_chunkAccessLock) {
            SetPermutationLocked(x, y, z, permutation, layer, dirty, broadcast);
        }
    }

    private void SetPermutationLocked(int x, int y, int z, BlockPermutation permutation, int layer, bool dirty, bool broadcast) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);

        BlockPermutation previous = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
        bool wasFluid = previous.Type.Liquid && !permutation.Type.Liquid;

        if (layer == 0 && !string.Equals(previous.Type.Identifier, permutation.Type.Identifier, StringComparison.Ordinal)) {
            CancelBlockTick(new BlockPos { X = x, Y = y, Z = z });
        }

        chunk.SetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), permutation, layer, dirty);

        BlockPos position = new() { X = x, Y = y, Z = z };
        if (permutation.Type.Traits.Count > 0) {
            Block? block = chunk.GetBlockActor(position);
            if (block is null || block.Type.Identifier != permutation.Type.Identifier) {
                block = new Block(permutation);
                chunk.SetBlockActor(position, block);
            }
            else {
                block.SetPermutation(permutation);
            }

            if (block.Interactable || permutation.IsComponentBased) {
                BlockLevelStorage storage = GetOrCreateBlockStorage(chunk, position, permutation.Type.Identifier);
                chunk.SetBlockStorage(position, storage, dirty);
            }
            else {
                chunk.SetBlockStorage(position, null, dirty);
            }
        }
        else {
            chunk.SetBlockActor(position, null);
            chunk.SetBlockStorage(position, null, dirty);
        }

        if (broadcast) {
            Broadcast(new UpdateBlockPacket {
                BlockPosition = position,
                BlockRuntimeID = (uint)permutation.NetworkId,
                Flags = (uint)(
                    UpdateBlockFlagsType.Neighbors |
                    UpdateBlockFlagsType.Network
                ),
                Layer = (uint)layer
            },
            new BroadcastOptions {
                Radius = World?.Server?.Properties.MaxViewDistance * 16 ?? 256,
            });
        }

        if (wasFluid) {
            FluidKind? kind = FluidTrait.GetFluidKind(previous);
            if (kind.HasValue) {
                FluidTrait.NotifyFluidNeighbors(kind.Value, this, position);
            }
        }
    }

    public Block? GetBlock(int x, int y, int z) {
        ChunkColumn? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null) {
            return null;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };
        Block? block = chunk.GetBlockActor(position);
        if (block is not null) {
            return block;
        }

        BlockPermutation perm = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z));
        if (perm.Type.Traits.Count > 0) {
            block = new Block(perm);
            BlockLevelStorage? storage = chunk.GetBlockStorage(position);
            if (storage is not null) {
                block.ReadTraits(storage);
            }

            chunk.SetBlockActor(position, block);
            return block;
        }

        return null;
    }

    public bool ScheduleBlockTick(BlockPos position, uint delay) {
        if (_disposed || World?.Scheduler is not { } scheduler) {
            return false;
        }

        long chunkHash = HashChunk(position.X >> 4, position.Z >> 4);
        if (!_chunks.TryGetValue(chunkHash, out ChunkColumn? chunk)) {
            return false;
        }

        var key = (position.X, position.Y, position.Z);
        if (_blockTicks.ContainsKey(key)) {
            return false;
        }

        BlockPermutation permutation = chunk.GetPermutation(GetChunkLocal(position.X), position.Y, GetChunkLocal(position.Z));
        BlockTickTask task = new(this, position, permutation.Type.Identifier, delay);
        _blockTicks[key] = task;
        scheduler.Schedule(task);
        return true;
    }

    private void CancelBlockTick(BlockPos position) {
        var key = (position.X, position.Y, position.Z);
        if (_blockTicks.Remove(key, out BlockTickTask? task)) {
            task.Cancel();
        }
    }

    internal void ExecuteBlockTick(BlockTickTask task) {
        BlockPos position = task.Position;
        var key = (position.X, position.Y, position.Z);
        if (!_blockTicks.TryGetValue(key, out BlockTickTask? scheduled) || !ReferenceEquals(task, scheduled)) {
            return;
        }

        _blockTicks.Remove(key);

        long chunkHash = HashChunk(position.X >> 4, position.Z >> 4);
        if (!_chunks.TryGetValue(chunkHash, out ChunkColumn? chunk) || !chunk.Simulated) {
            return;
        }

        BlockPermutation permutation = chunk.GetPermutation(GetChunkLocal(position.X), position.Y, GetChunkLocal(position.Z));
        if (!string.Equals(permutation.Type.Identifier, task.BlockIdentifier, StringComparison.Ordinal)) {
            return;
        }

        Block? block = GetBlock(position.X, position.Y, position.Z);
        block?.OnTick(new BlockTickDetails(this, position));
    }

    private void RestoreBlockTicks(ChunkColumn chunk) {
        int subChunkOffset = Type == DimensionId.Overworld ? 4 : 0;

        for (int subChunkIndex = 0; subChunkIndex < chunk.SubChunks.Length; subChunkIndex++) {
            Chunk.SubChunk? subChunk = chunk.SubChunks[subChunkIndex];
            if (subChunk is null || subChunk.Layers.Count == 0) {
                continue;
            }

            Chunk.BlockStorage storage = subChunk.Layers[0];
            bool tickablePalette = false;
            for (int paletteIndex = 0; paletteIndex < storage.Palette.Count; paletteIndex++) {
                BlockType type = BlockPermutation.Resolve(storage.Palette[paletteIndex]).Type;
                if (HasTrait<CropTrait>(type) || HasTrait<FarmlandTrait>(type)) {
                    tickablePalette = true;
                    break;
                }
            }

            if (!tickablePalette) {
                continue;
            }

            int subChunkY = subChunk.Index ?? subChunkIndex - subChunkOffset;
            for (int x = 0; x < 16; x++) {
                for (int z = 0; z < 16; z++) {
                    for (int y = 0; y < 16; y++) {
                        BlockPermutation permutation = BlockPermutation.Resolve(storage.GetState(x, y, z));
                        BlockPos position = new() {
                            X = (chunk.X << 4) + x,
                            Y = (subChunkY << 4) + y,
                            Z = (chunk.Z << 4) + z
                        };

                        if (HasTrait<FarmlandTrait>(permutation.Type)) {
                            FarmlandTrait.ScheduleFarmlandTick(this, position);
                            continue;
                        }

                        if (!HasTrait<CropTrait>(permutation.Type) ||
                            !permutation.State.TryGetValue(CropTrait.State, out BlockStateValue growth) ||
                            growth.Kind != 0 || growth.AsNumber() >= 7) {
                            continue;
                        }

                        CropTrait.ScheduleCropTick(this, position);
                    }
                }
            }
        }
    }

    private static bool HasTrait<T>(BlockType type) where T : BlockTrait {
        foreach (System.Type traitType in type.Traits.Values) {
            if (traitType == typeof(T)) {
                return true;
            }
        }

        return false;
    }

    public void SetBlock(int x, int y, int z, Block block) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBlockActor(new BlockPos { X = x, Y = y, Z = z }, block);
    }

    public void RemoveBlock(int x, int y, int z) {
        ChunkColumn? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null) {
            return;
        }

        chunk.SetBlockActor(new BlockPos { X = x, Y = y, Z = z }, null);
    }

    public int GetBiome(int x, int y, int z) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetBiome(GetChunkLocal(x), y, GetChunkLocal(z));
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBiome(GetChunkLocal(x), y, GetChunkLocal(z), biomeId, dirty);
    }

    /// <summary>
    /// Fills a region with the given permutations.
    /// All of the updates are sent via UpdateSubChunkBlocks packet rather than UpdateBlockPacket
    /// </summary>
    public int Fill(int minX, int minY, int minZ, int maxX, int maxY, int maxZ, BlockPermutation permutation) {
        int filled = 0;
        Dictionary<(int cx, int cy, int cz), List<UpdateSubChunkNetworkBlockInfo>> subChunkEntries = [];

        for (int x = minX; x <= maxX; x++) {
            for (int z = minZ; z <= maxZ; z++) {
                for (int y = minY; y <= maxY; y++) {
                    SetPermutation(x, y, z, permutation, broadcast: false);
                    filled++;

                    int cx = x >> 4;
                    int cy = y >> 4;
                    int cz = z >> 4;
                    var key = (cx, cy, cz);

                    if (!subChunkEntries.TryGetValue(key, out List<UpdateSubChunkNetworkBlockInfo>? entries)) {
                        entries = [];
                        subChunkEntries[key] = entries;
                    }

                    entries.Add(new UpdateSubChunkNetworkBlockInfo {
                        Pos = new BlockPos() {
                            X = x,
                            Y = y,
                            Z = z,
                        },
                        RuntimeId = (uint)permutation.NetworkId,
                        SyncMessageEntityUniqueID = 0,
                        SyncMessageMessage = 0,
                        UpdateFlags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Neighbors),
                    });
                }
            }
        }

        float broadcastRadius = World?.Server?.Properties.MaxViewDistance * 16 ?? 256;
        foreach (((int scx, int scy, int scz), List<UpdateSubChunkNetworkBlockInfo> entries) in subChunkEntries) {
            Broadcast(new UpdateSubChunkBlocksPacket {
                SubChunkBlockPosition = new BlockPos() {
                    X = scx,
                    Y = scy,
                    Z = scz,
                },
                BlocksChanged = new UpdateSubChunkBlocksChangedInfo() {
                    BlocksChangedStandards = entries,
                    BlocksChangedExtras = new List<UpdateSubChunkNetworkBlockInfo>()
                },

                // SubChunkX = scx,
                // SubChunkY = scy,
                // SubChunkZ = scz,
                // Blocks = entries
            }, new BroadcastOptions {
                Radius = broadcastRadius,
                Center = new Vec3 {
                    X = (scx << 4) + 8,
                    Y = (scy << 4) + 8,
                    Z = (scz << 4) + 8
                }
            });
        }

        return filled;
    }

    public void Dispose() {
        _disposed = true;
        foreach (BlockTickTask task in _blockTicks.Values) {
            task.Cancel();
        }
        _blockTicks.Clear();
        FlushCompletedChunkRequests(int.MaxValue);
        _provider.SaveSpawnPosition(Type, SpawnPosition);

        foreach (ChunkColumn chunk in _chunks.Values) {
            SyncBlockActorsToStorages(chunk);
            SyncEntitiesToStorage(chunk);
            if (!chunk.Dirty) {
                continue;
            }

            chunk.Dirty = false;
            if (World is { } world) {
                world.Persistence.SaveChunk(chunk);
            }
            else {
                _provider.SaveChunk(chunk);
            }
        }

        _chunks.Clear();
    }

    public void Tick(ulong currentTick, uint deltaTick) {
        using var __tick = Profiler.Enabled ? Profiler.BeginZone("Dimension.Tick") : default;

        using (Profiler.Enabled ? Profiler.BeginZone("FlushCompletedChunks") : default) {
            FlushCompletedChunkRequests(CompletedChunkLimit);
        }

        if (currentTick % 20 == 0 && _pendingUnloads.Count > 0) {
            using var unloadZone = Profiler.Enabled ? Profiler.BeginZone("UnloadUnviewedChunks") : default;
            int unloadLimit = Math.Min(_pendingUnloads.Count, 256);
            _chunkSweepBuffer.Clear();

            foreach (long hash in _pendingUnloads) {
                if (hash == HashChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z))) {
                    continue;
                }

                if (_chunkViewers.ContainsKey(hash)) {
                    continue;
                }

                _chunkSweepBuffer.Add(hash);
                if (_chunkSweepBuffer.Count >= unloadLimit) {
                    break;
                }
            }

            for (int i = 0; i < _chunkSweepBuffer.Count; i++) {
                long hash = _chunkSweepBuffer[i];
                _pendingUnloads.Remove(hash);
                int x = (int)(hash >> 32);
                int z = (int)hash;
                UnloadChunk(x, z, save: true);
            }
        }

        if (_entities.Count == 0) {
            return;
        }

        bool simulationChanged = false;
        int simulationDistance = 0;
        _simulationPlayerBuffer.Clear();

        if (World?.Server is Server server) {
            simulationDistance = Math.Clamp(server.Properties.SimulationDistance, 0, 120);

            foreach ((_, var player) in server.Players) {
                if (player.Dimension != this) {
                    continue;
                }

                _simulationPlayerBuffer.Add(player);
                long hash = HashChunk(
                    WorldToChunk(player.Position.X),
                    WorldToChunk(player.Position.Z)
                );

                if (!_simulationPlayerChunks.TryGetValue(player, out long previous) || previous != hash) {
                    _simulationPlayerChunks[player] = hash;
                    simulationChanged = true;
                }
            }
        }

        if (_simulationPlayerChunks.Count != _simulationPlayerBuffer.Count) {
            _simulationPlayerRemovalBuffer.Clear();
            foreach (Player.Player player in _simulationPlayerChunks.Keys) {
                if (!_simulationPlayerBuffer.Contains(player)) {
                    _simulationPlayerRemovalBuffer.Add(player);
                }
            }

            for (int i = 0; i < _simulationPlayerRemovalBuffer.Count; i++) {
                _simulationPlayerChunks.Remove(_simulationPlayerRemovalBuffer[i]);
            }

            simulationChanged = true;
        }

        if (_simulationDistance != simulationDistance) {
            _simulationDistance = simulationDistance;
            simulationChanged = true;
        }

        if (simulationChanged) {
            using var simulationZone = Profiler.Enabled ? Profiler.BeginZone("Dimension.UpdateSimulation") : default;
            _simulationChunkBuffer.Clear();

            foreach (long playerChunk in _simulationPlayerChunks.Values) {
                int currentChunkX = (int)(playerChunk >> 32);
                int currentChunkZ = (int)playerChunk;

                for (int dx = -simulationDistance; dx <= simulationDistance; dx++) {
                    for (int dz = -simulationDistance; dz <= simulationDistance; dz++) {
                        _simulationChunkBuffer.Add(HashChunk(currentChunkX + dx, currentChunkZ + dz));
                    }
                }
            }

            foreach (long hash in _simulatedChunks) {
                if (!_simulationChunkBuffer.Contains(hash) &&
                    _chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                    chunk.Simulated = false;
                }
            }

            foreach (long hash in _simulationChunkBuffer) {
                if (!_simulatedChunks.Contains(hash) &&
                    _chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                    chunk.Simulated = true;
                    RestoreBlockTicks(chunk);
                }
            }

            _simulatedChunks.Clear();
            _simulatedChunks.UnionWith(_simulationChunkBuffer);
        }

        _tickEntityBuffer.Clear();
        foreach (long hash in _simulatedChunks) {
            if (_chunkEntities.TryGetValue(hash, out HashSet<Entity>? entities)) {
                _tickEntityBuffer.AddRange(entities);
            }
        }

        _tickingEntities = true;
        using (Profiler.Enabled ? Profiler.BeginZone("Dimension.TickEntities") : default) {
            foreach (Entity entity in _tickEntityBuffer) {
                if (entity.PendingDespawn || entity.Dimension != this) {
                    _pendingEntityRemoves.Add(entity);
                    continue;
                }

                if (entity.Position.Y < VoidY) {
                    if (entity is ItemEntity) {
                        entity.Despawn(new EntityDespawnOptions());
                    }
                    else if (currentTick >= entity.NextVoidDamageTick && entity.GetTrait<EntityHealthTrait>() is { } health) {
                        entity.NextVoidDamageTick = currentTick + VoidDamageCooldownTicks;
                        health.ApplyDamage(float.MaxValue, null, ActorDamageCause.Void);
                    }

                    continue;
                }

                entity.Tick(currentTick, deltaTick);
            }
        }
        _tickingEntities = false;
        FlushPendingEntityChanges();
    }

    public void Broadcast(Packet packet, BroadcastOptions? options = null) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Dimension.Broadcast") : default;
        if (World?.Server is not Server server) {
            return;
        }

        BroadcastOptions resolved = options ?? new BroadcastOptions();
        resolved.Center ??= GetPacketPosition(packet);
        float radiusSquared = resolved.Radius * resolved.Radius;

        foreach ((var connection, var player) in server.Players) {
            if (player.Dimension != this) {
                continue;
            }

            if (resolved.Except is not null && resolved.Except.Contains(player)) {
                continue;
            }

            if (resolved.Center is not null) {
                Vec3 playerPosition = player.Position;
                Vec3 centerPosition = resolved.Center;
                float dx = playerPosition.X - centerPosition.X;
                float dy = playerPosition.Y - centerPosition.Y;
                float dz = playerPosition.Z - centerPosition.Z;
                float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                if (distanceSquared > radiusSquared) {
                    continue;
                }
            }

            server.Network.QueuePacket(connection, packet);
        }
    }

    public void PlaySound(
        string soundEvent,
        Vec3 position,
        float radius = 64f,
        int data = 0,
        string actorIdentifier = "",
        bool babyMob = false,
        bool disableRelativeVolume = false,
        long uniqueActorId = 0,
        Vec3? fireAtPosition = null) {
        Broadcast(new LevelSoundEventPacket {
            SoundEvent = soundEvent,
            Position = position,
            Data = data,
            ActorIdentifier = actorIdentifier,
            IsBaby = babyMob,
            IsGlobal = disableRelativeVolume,
            ActorUniqueId = uniqueActorId,
            FireAtPosition = fireAtPosition
        }, new BroadcastOptions {
            Center = position,
            Radius = radius
        });
    }

    internal void AddEntity(Entity entity) {
        if (_tickingEntities) {
            _pendingEntityRemoves.Remove(entity);
            _pendingEntityAdds.Add(entity);
            UpdateEntityStorage(entity);
            return;
        }

        _entities.Add(entity);
        IndexEntity(entity);
        UpdateEntityStorage(entity);
        UpdateEntityVisibility(entity);
    }

    internal void RemoveEntity(Entity entity, bool complete = true) {
        if (_tickingEntities) {
            _pendingEntityAdds.Remove(entity);
            _pendingEntityRemoves.Add(entity);
            return;
        }

        RemoveEntityStorage(entity);
        UnindexEntity(entity);
        if (complete) {
            entity.CompleteDespawn();
        }
        _entities.Remove(entity);
    }

    public void AddPlayer(Player.Player joining) {
        if (World?.Server is not Server server) {
            return;
        }

        ulong tick = World is Tickable tickable ? tickable.TickValue : 0;
        PlayerChunkRenderingTrait? joiningRenderer = joining.GetTrait<PlayerChunkRenderingTrait>();
        foreach ((_, Player.Player other) in server.Players) {
            if (ReferenceEquals(other, joining) || other.Dimension != this || !other.IsAlive ||
                !InEntityVisibilityRange(joining.Location, other.Location)) {
                continue;
            }

            if (joiningRenderer?.VisibleActorIds.TryAdd(other.RuntimeId, other.UniqueId) != false) {
                other.SpawnTo(joining, tick);
            }

            PlayerChunkRenderingTrait? otherRenderer = other.GetTrait<PlayerChunkRenderingTrait>();
            if (otherRenderer?.VisibleActorIds.TryAdd(joining.RuntimeId, joining.UniqueId) != false) {
                joining.SpawnTo(other, tick);
            }
        }
    }

    public void RemovePlayer(Player.Player leaving) {
        HideEntity(leaving);
    }

    internal void UpdatePlayerVisibility(Player.Player moving) {
        if (World?.Server is not Server server) {
            return;
        }

        PlayerChunkRenderingTrait? movingRenderer = moving.GetTrait<PlayerChunkRenderingTrait>();
        foreach (Player.Player other in server.Players.Values) {
            if (ReferenceEquals(other, moving) || other.Dimension != this) {
                continue;
            }

            movingRenderer?.UpdateVisibleEntity(other);
            other.GetTrait<PlayerChunkRenderingTrait>()?.UpdateVisibleEntity(moving);
        }
    }

    private static long HashChunk(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }

    private static bool InEntityVisibilityRange(Vec3 first, Vec3 second) {
        float dx = first.X - second.X;
        float dy = first.Y - second.Y;
        float dz = first.Z - second.Z;
        return (dx * dx) + (dy * dy) + (dz * dz) <= 64f * 64f;
    }

    private static int WorldToChunk(float coordinate) {
        return (int)MathF.Floor(coordinate) >> 4;
    }

    private ChunkColumn? GetOrLoadChunk(int x, int z) {
        lock (_chunkAccessLock) {
            long hash = HashChunk(x, z);
            if (_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                return chunk;
            }

            World?.Persistence.WaitForChunk(Type, x, z);
            chunk = _provider.LoadChunk(Type, x, z);
            if (chunk is not null) {
                chunk.Simulated = _simulatedChunks.Contains(hash);
                _chunks[hash] = chunk;
                MaterializeEntities(chunk);
                if (chunk.Simulated) {
                    RestoreBlockTicks(chunk);
                }
            }

            return chunk;
        }
    }

    private static int GetChunkLocal(int value) {
        return value & 0xF;
    }

    private void FlushPendingEntityChanges() {
        if (_pendingEntityRemoves.Count > 0) {
            foreach (Entity entity in _pendingEntityRemoves) {
                RemoveEntityStorage(entity);
                UnindexEntity(entity);
                if (entity.Dimension == this) {
                    entity.CompleteDespawn();
                }
                _entities.Remove(entity);
            }

            _pendingEntityRemoves.Clear();
        }

        if (_pendingEntityAdds.Count > 0) {
            foreach (Entity entity in _pendingEntityAdds) {
                _entities.Add(entity);
                IndexEntity(entity);
                UpdateEntityVisibility(entity);
            }

            _pendingEntityAdds.Clear();
        }
    }

    private static BlockLevelStorage GetOrCreateBlockStorage(ChunkColumn chunk, BlockPos position, string blockIdentifier) {
        BlockLevelStorage? storage = chunk.GetBlockStorage(position);
        if (storage is not null) {
            return storage;
        }

        storage = new BlockLevelStorage(chunk);
        storage.SetPosition(position);
        storage.Set("id", new StringTag { Name = "id", Value = GetBlockActorId(blockIdentifier) });
        storage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
        return storage;
    }

    private static void SyncBlockActorsToStorages(ChunkColumn chunk) {
        foreach (KeyValuePair<(int X, int Y, int Z), Block> actorEntry in chunk.GetAllBlockActors()) {
            BlockPos position = new() {
                X = actorEntry.Key.X,
                Y = actorEntry.Key.Y,
                Z = actorEntry.Key.Z
            };

            BlockLevelStorage storage = GetOrCreateBlockStorage(chunk, position, actorEntry.Value.Type.Identifier);
            actorEntry.Value.WriteTraits(storage);
            chunk.SetBlockStorage(position, storage, dirty: true);
        }
    }

    internal void UpdateEntityStorage(Entity entity) {
        if (entity.Dimension != this) {
            return;
        }

        if (entity.PendingDespawn) {
            RemoveEntityStorage(entity);
            UnindexEntity(entity);
            return;
        }

        bool changedChunk = UpdateEntityIndex(entity);

        if (changedChunk) {
            UpdateEntityVisibility(entity);
        }

        if (entity is Player.Player player && player.Xuid.Length > 0) {
            return;
        }

        long hash = HashChunk(WorldToChunk(entity.Position.X), WorldToChunk(entity.Position.Z));
        if (_entityChunks.TryGetValue(entity, out long previousHash)) {
            if (previousHash == hash) {
                if (_chunks.TryGetValue(hash, out ChunkColumn? currentChunk)) {
                    currentChunk.Dirty = true;
                }
                return;
            }

            if (_chunks.TryGetValue(previousHash, out ChunkColumn? previousChunk)) {
                previousChunk.SetEntityStorage(entity.UniqueId, null);
            }
        }

        ChunkColumn chunk = GetOrCreateChunk(
            WorldToChunk(entity.Position.X),
            WorldToChunk(entity.Position.Z)
        );
        chunk.SetEntityStorage(entity.UniqueId, entity.Write());
        _entityChunks[entity] = hash;
    }

    private void RemoveEntityStorage(Entity entity) {
        if (entity is Player.Player player && player.Xuid.Length > 0) {
            return;
        }

        if (_entityChunks.Remove(entity, out long hash) &&
            _chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
            chunk.SetEntityStorage(entity.UniqueId, null);
            return;
        }

        int chunkX = WorldToChunk(entity.Position.X);
        int chunkZ = WorldToChunk(entity.Position.Z);
        if (_chunks.TryGetValue(HashChunk(chunkX, chunkZ), out ChunkColumn? currentChunk)) {
            currentChunk.SetEntityStorage(entity.UniqueId, null);
        }
    }

    private void IndexEntity(Entity entity) {
        long hash = HashChunk(WorldToChunk(entity.Position.X), WorldToChunk(entity.Position.Z));
        _entityChunkIndexes[entity] = hash;

        if (!_chunkEntities.TryGetValue(hash, out HashSet<Entity>? entities)) {
            entities = [];
            _chunkEntities[hash] = entities;
        }

        entities.Add(entity);
    }

    private bool UpdateEntityIndex(Entity entity) {
        long hash = HashChunk(WorldToChunk(entity.Position.X), WorldToChunk(entity.Position.Z));
        if (_entityChunkIndexes.TryGetValue(entity, out long previousHash)) {
            if (previousHash == hash) {
                return false;
            }

            if (_chunkEntities.TryGetValue(previousHash, out HashSet<Entity>? previousEntities)) {
                previousEntities.Remove(entity);
                if (previousEntities.Count == 0) {
                    _chunkEntities.Remove(previousHash);
                }
            }
        }

        _entityChunkIndexes[entity] = hash;
        if (!_chunkEntities.TryGetValue(hash, out HashSet<Entity>? entities)) {
            entities = [];
            _chunkEntities[hash] = entities;
        }

        entities.Add(entity);
        return true;
    }

    private void UnindexEntity(Entity entity) {
        if (!_entityChunkIndexes.Remove(entity, out long hash) ||
            !_chunkEntities.TryGetValue(hash, out HashSet<Entity>? entities)) {
            return;
        }

        entities.Remove(entity);
        if (entities.Count == 0) {
            _chunkEntities.Remove(hash);
        }
    }

    internal void UpdateEntityVisibility(Entity entity) {
        if (World?.Server is not Server server) {
            return;
        }

        foreach (Player.Player player in server.Players.Values) {
            if (player.Dimension == this) {
                player.GetTrait<PlayerChunkRenderingTrait>()?.UpdateVisibleEntity(entity);
            }
        }
    }

    internal void HideEntity(Entity entity) {
        if (World?.Server is not Server server) {
            return;
        }

        foreach (Player.Player player in server.Players.Values) {
            if (player.Dimension == this) {
                player.GetTrait<PlayerChunkRenderingTrait>()?.HideVisibleEntity(entity);
            }
        }
    }

    private void SyncEntitiesToStorage(ChunkColumn chunk) {
        foreach (Entity entity in _entities) {
            if (entity is Player.Player player && player.Xuid.Length > 0) {
                continue;
            }

            if (entity.PendingDespawn || entity.Dimension != this) {
                RemoveEntityStorage(entity);
                continue;
            }

            long hash = HashChunk(WorldToChunk(entity.Position.X), WorldToChunk(entity.Position.Z));
            if (_entityChunks.TryGetValue(entity, out long previousHash) &&
                previousHash != hash &&
                _chunks.TryGetValue(previousHash, out ChunkColumn? previousChunk)) {
                previousChunk.SetEntityStorage(entity.UniqueId, null);
            }

            if (hash != chunk.Hash) {
                continue;
            }

            chunk.SetEntityStorage(entity.UniqueId, entity.Write());
            _entityChunks[entity] = hash;
        }
    }

    private void MaterializeEntities(ChunkColumn chunk) {
        List<KeyValuePair<long, CompoundTag>> storedEntities = chunk.GetAllEntityStorages();
        for (int i = 0; i < storedEntities.Count; i++) {
            KeyValuePair<long, CompoundTag> stored = storedEntities[i];
            Entity? existing = _entities.FirstOrDefault(entity => entity.UniqueId == stored.Key);
            if (existing is not null) {
                _entityChunks[existing] = HashChunk(
                    WorldToChunk(existing.Position.X),
                    WorldToChunk(existing.Position.Z)
                );
                UpdateEntityIndex(existing);
                continue;
            }

            CompoundTag tag = stored.Value;
            string? identifier = tag.Get<StringTag>("identifier")?.Value;
            ListTag? position = tag.Get<ListTag>("Pos");
            bool positionStored =
                tag.Get<FloatTag>("x") is not null &&
                tag.Get<FloatTag>("y") is not null &&
                tag.Get<FloatTag>("z") is not null;
            if (string.IsNullOrWhiteSpace(identifier) ||
                (!positionStored && position is not { Values.Count: >= 3 }) ||
                string.Equals(identifier, EntityIdentifier.Player.ToIdentifierString(), StringComparison.Ordinal)) {
                continue;
            }

            try {
                Entity? entity;
                if (string.Equals(identifier, "minecraft:item", StringComparison.Ordinal)) {
                    CompoundTag? itemTag = tag.Get<CompoundTag>("item");
                    ItemStack? item = itemTag is null ? null : ItemStack.Deserialize(itemTag);
                    entity = item is null ? null : new Basalt.Core.Entities.ItemEntity(item);
                }
                else {
                    entity = EntityFactory.Create(identifier, tag) ?? new Entity(identifier);
                }

                if (entity is null) {
                    continue;
                }

                entity.RestoreUniqueId(stored.Key);
                entity.Read(tag);
                entity.Spawn(this, new EntitySpawnOptions(InitialSpawn: true));
            }
            catch (Exception exception) {
                Logger.Warn($"Failed materializing entity {stored.Key} in chunk {chunk.X},{chunk.Z}: {exception}");
            }
        }
    }

    private void UnloadEntities(ChunkColumn chunk) {
        List<Entity> unloaded = [];
        foreach (Entity entity in _entities) {
            if ((entity is Player.Player player && player.Xuid.Length > 0) ||
                WorldToChunk(entity.Position.X) != chunk.X ||
                WorldToChunk(entity.Position.Z) != chunk.Z) {
                continue;
            }

            unloaded.Add(entity);
        }

        for (int i = 0; i < unloaded.Count; i++) {
            Entity entity = unloaded[i];
            _pendingEntityAdds.Remove(entity);
            _pendingEntityRemoves.Remove(entity);
            _entities.Remove(entity);
            _entityChunks.Remove(entity);
            UnindexEntity(entity);
            entity.CompleteDespawn();
        }
    }

    internal static string GetBlockActorId(string blockIdentifier) {
        return BlockActorIds.TryGetValue(blockIdentifier, out string? value) ? value : blockIdentifier;
    }

    private static Vec3? GetPacketPosition(Packet packet) {
        switch (packet) {
            case UpdateBlockPacket updateBlock:
                return ToVec3(updateBlock.BlockPosition.X, updateBlock.BlockPosition.Y, updateBlock.BlockPosition.Z);

            case BlockActorDataPacket blockActor:
                return ToVec3(blockActor.BlockPosition.X, blockActor.BlockPosition.Y, blockActor.BlockPosition.Z);

            case LevelEventPacket levelEvent:
                return levelEvent.Position;

            case BlockEventPacket blockEvent:
                return ToVec3(blockEvent.BlockPosition.X, blockEvent.BlockPosition.Y, blockEvent.BlockPosition.Z);

            case LevelSoundEventPacket levelSoundEvent:
                return levelSoundEvent.Position;

            case MovePlayerPacket movePlayer:
                return movePlayer.Position;

            case MoveActorDeltaPacket moveActorDelta:
                return new Vec3() {
                    X = moveActorDelta.MoveData.NewPositionX ?? 0,
                    Y = moveActorDelta.MoveData.NewPositionX ?? 0,
                    Z = moveActorDelta.MoveData.NewPositionX ?? 0,
                };

            default:
                return null;
        }
    }

    private static Vec3 ToVec3(float x, float y, float z) {
        return new Vec3 { X = x, Y = y, Z = z };
    }

    private void FlushCompletedChunkRequests(int limit) {
        int completed = 0;
        while (completed < limit && _chunkRequestCallbacks.TryDequeue(out ChunkRequestCallback ready)) {
            ready.Callback(ready.Chunk);
            completed++;
        }
    }

    private void HandleChunkCompleted(long hash, ChunkColumn? chunk) {
        PendingChunkRequest? request;
        lock (_chunkRequestLock) {
            if (!_pendingChunkRequests.Remove(hash, out request)) {
                return;
            }

            if (_chunkViewers.TryGetValue(hash, out int count)) {
                if (count <= 1) {
                    _chunkViewers.Remove(hash);
                    _pendingUnloads.Add(hash);
                }
                else {
                    _chunkViewers[hash] = count - 1;
                }
            }
        }

        if (chunk is null) {
            return;
        }

        if (_chunks.TryGetValue(hash, out ChunkColumn? existing)) {
            foreach (Action<ChunkColumn> callback in request.Callbacks) {
                callback(existing);
            }
        }
        else {
            chunk.Simulated = _simulatedChunks.Contains(hash);
            _chunks[hash] = chunk;
            MaterializeEntities(chunk);
            if (chunk.Simulated) {
                RestoreBlockTicks(chunk);
            }

            foreach (Action<ChunkColumn> callback in request.Callbacks) {
                callback(chunk);
            }
        }
    }

    private sealed class PendingChunkRequest {
        public readonly List<Action<ChunkColumn>> Callbacks;

        public PendingChunkRequest(Action<ChunkColumn> callback) {
            Callbacks = [callback];
        }
    }

    private readonly record struct ChunkRequestCallback(ChunkColumn Chunk, Action<ChunkColumn> Callback);

    internal bool AutoSaving => _autoSaveChunks is not null;

    internal void BeginAutoSave() {
        if (World is { } world) {
            world.Persistence.SaveSpawnPosition(Type, SpawnPosition);
        }
        else {
            _provider.SaveSpawnPosition(Type, SpawnPosition);
        }
        _autoSaveChunks = [.. _chunks.Values];
        _autoSaveIndex = 0;
    }

    internal int AutoSave(int limit) {
        if (_autoSaveChunks is null || limit <= 0) {
            return 0;
        }

        int processed = 0;
        while (processed < limit && _autoSaveIndex < _autoSaveChunks.Length) {
            ChunkColumn chunk = _autoSaveChunks[_autoSaveIndex++];
            processed++;
            if (!_chunks.TryGetValue(chunk.Hash, out ChunkColumn? loaded) || !ReferenceEquals(chunk, loaded)) {
                continue;
            }

            SyncBlockActorsToStorages(chunk);
            SyncEntitiesToStorage(chunk);
            if (!chunk.Dirty) {
                continue;
            }

            try {
                ChunkColumn snapshot = chunk.CreatePersistenceSnapshot();
                chunk.Dirty = false;
                if (World is { } world) {
                    world.Persistence.SaveChunk(snapshot);
                }
                else {
                    _provider.SaveChunk(snapshot);
                }
            }
            catch (Exception exception) {
                Logger.Err($"Failed to save chunk {chunk.X},{chunk.Z}: {exception.Message}");
            }
        }

        if (_autoSaveIndex >= _autoSaveChunks.Length) {
            _autoSaveChunks = null;
        }

        return processed;
    }
}

[Flags]
public enum UpdateBlockFlagsType : uint {
    None = 0,
    Neighbors = 1,
    Network = 2,
    NoGraphic = 4,
    Priority = 8
}
