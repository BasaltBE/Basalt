using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Enums;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Network;
using Basalt.Core.Pathfinding;
using Basalt.Core.Player.Traits;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;
using Entity = Basalt.Core.Entities.Entity;
using Path = Basalt.Core.Pathfinding.Path;
using TaskScheduler = Basalt.Core.Tasks.TaskScheduler;

namespace Basalt.Core.Worlds.Dimensions;

public sealed class Dimension : IDisposable {
    internal const int DefaultRegionChunkSize = 8;
    private const int CompletedChunkLimit = 128;
    private const int DomainMailboxCapacity = 4096;
    private const int MaxDomainCommandsPerTick = 512;
    private const int MaxPendingChunkRequests = 512;
    private const float VoidY = -64f;
    private const ulong VoidDamageCooldownTicks = 20;

    private static readonly FrozenDictionary<string, string> BlockActorIds = new Dictionary<string, string> {
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
    }.ToFrozenDictionary(StringComparer.Ordinal);
    private static readonly BlockPermutation AirPermutation = BlockPermutation.Resolve("minecraft:air");

    private readonly Dictionary<long, ChunkColumn> _chunks;
    private readonly Dictionary<long, int> _chunkViewers;
    private readonly HashSet<long> _pendingUnloads = [];
    private readonly Dictionary<Entity, long> _entityChunks = [];
    private readonly Dictionary<Entity, EntityChunkIndex> _entityChunkIndexes = [];
    private readonly Dictionary<long, List<Entity>> _chunkEntities = [];
    private readonly Dictionary<Player.Player, long> _playerChunks = [];
    private readonly Dictionary<long, HashSet<Player.Player>> _playersByChunk = [];
    private readonly Dictionary<RegionCoordinate, List<Entity>> _tickRegionBuffers = [];
    private readonly Dictionary<Player.Player, long> _simulationPlayerChunks = [];
    private readonly Dictionary<long, int> _simulationChunkReferences = [];
    private readonly Dictionary<(int X, int Y, int Z), BlockTickTask> _blockTicks = [];
    private readonly Dictionary<long, PendingChunkRequest> _pendingChunkRequests = [];

    private readonly HashSet<Entity> _entities;
    private readonly HashSet<Player.Player> _players;
    private readonly HashSet<Player.Player> _visibilityPlayerBuffer = [];
    private readonly HashSet<Entity> _pendingEntityAdds = [];
    private readonly HashSet<Entity> _pendingEntityRemoves = [];
    private readonly HashSet<Player.Player> _simulationPlayerBuffer = [];
    private readonly HashSet<long> _simulatedChunks = [];

    private readonly List<Entity> _tickEntityBuffer = [];
    private readonly List<Entity> _tickOwnedEntities = [];
    private readonly List<long> _chunkSweepBuffer = [];
    private readonly List<Player.Player> _simulationPlayerRemovalBuffer = [];

    private readonly ConcurrentDictionary<Entity, byte> _parallelEntityAdds = [];
    private readonly ConcurrentDictionary<Entity, byte> _parallelStorageUpdates = [];
    private readonly ConcurrentDictionary<Entity, byte> _parallelVisibilityUpdates = [];
    private readonly ConcurrentDictionary<Entity, byte> _parallelHiddenEntities = [];
    private readonly ConcurrentQueue<(Entity Entity, bool Complete)> _parallelEntityRemoves = new();
    private readonly ConcurrentQueue<ChunkRequestCallback> _chunkRequestCallbacks = new();

    private readonly object _chunkAccessLock = new();
    private readonly Lock _chunkRequestLock = new();
    private readonly ExecutionDomainMailbox _mailbox;
    private readonly WorldProvider _provider;
    private readonly Generator _generator;

    private Entity[] _entitiesSnapshot = [];
    private Player.Player[] _playersSnapshot = [];
    private ChunkColumn[]? _autoSaveChunks;
    private Vec3 _spawnPosition = new() {
        X = 0,
        Y = 80,
        Z = 0,
    };

    private World? _world;
    private bool _tickingEntities;
    private bool _disposed;
    private int _simulationDistance = -1;
    private int _ownerThreadId;
    private int _activePlayerCount;
    private int _activeEntityCount;
    private int _autoSaveIndex;
    private double _tickWork;
    private int _parallelRegionTicking;

    internal int RegionChunkSize { get; set; } = DefaultRegionChunkSize;

    #region Properties

    public string Identifier { get; }
    public DimensionId Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public World? World {
        get => _world;
        internal set {
            _world = value;
            if (value is not null) {
                GetOrCreateChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z));
            }
        }
    }
    public DimensionGameRules Gamerules { get; } = new();

    public Vec3 SpawnPosition {
        get => _spawnPosition;
        set {
            _spawnPosition = value;
            if (World is not null) {
                GetOrCreateChunk(WorldToChunk(value.X), WorldToChunk(value.Z));
            }
        }
    }

    public bool IsDay() => (World?.CurrentDayTime ?? 0) < 12000;
    public bool IsNight() => !IsDay();

    public Dimension(string identifier, DimensionId type, WorldProvider provider, Generator? generator = null) {
        Identifier = identifier;
        Type = type;
        _chunks = [];
        _chunkViewers = [];
        _entities = [];
        _players = [];
        _mailbox = new ExecutionDomainMailbox(DomainMailboxCapacity);
        _provider = provider;
        _generator = generator ?? new VoidGenerator();
    }

    public int ChunkCount {
        get {
            lock (_chunkAccessLock) {
                return _chunks.Count;
            }
        }
    }

    public int ChunkViewerCount {
        get {
            lock (_chunkRequestLock) {
                return _chunkViewers.Count;
            }
        }
    }
    public int ActivePlayerCount => Volatile.Read(ref _activePlayerCount);
    public int ActiveEntityCount => Volatile.Read(ref _activeEntityCount);
    public int PendingChunkRequestCount {
        get {
            lock (_chunkRequestLock) {
                return _pendingChunkRequests.Count;
            }
        }
    }
    public int PendingChunkCallbackCount => _chunkRequestCallbacks.Count;
    public double TickWork => Volatile.Read(ref _tickWork);

    #endregion

    #region Execution

    internal IReadOnlyCollection<Entity> Entities => _entities;
    public Entity[] GetEntitiesSnapshot() => Volatile.Read(ref _entitiesSnapshot);
    public IReadOnlyCollection<Player.Player> GetPlayers() => Volatile.Read(ref _playersSnapshot);
    public Player.Player[] GetPlayersSnapshot() => [.. Volatile.Read(ref _playersSnapshot)];

    private void PublishPlayersSnapshot() {
        Volatile.Write(ref _entitiesSnapshot, [.. _entities]);
        Volatile.Write(ref _playersSnapshot, [.. _players]);
        World?.MarkPlayersSnapshotDirty();
        PublishCounts();
    }

    public bool TryEnqueue(Action command) {
        ArgumentNullException.ThrowIfNull(command);
        if (_disposed) {
            return false;
        }

        return _mailbox.TryEnqueue(command);
    }

    public bool TryEnqueue(Player.Player player, Action command) {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(command);
        return TryEnqueue(() => {
            if (ReferenceEquals(player.Dimension, this)) {
                command();
            }
        });
    }

    internal bool TryEnqueueRegion(Entity entity, RegionCoordinate region, Action command) {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(command);
        return TryEnqueue(() => {
            if (entity.Dimension != this ||
                GetRegionCoordinate(
                    WorldToChunk(entity.Position.X),
                    WorldToChunk(entity.Position.Z),
                    RegionChunkSize) != region) {
                return;
            }

            command();
        });
    }

    internal bool TryEnqueueCoalesced(object key, Action command) {
        return !_disposed && _mailbox.TryEnqueueCoalesced(key, command);
    }

    internal bool IsOwnerThread => Volatile.Read(ref _ownerThreadId) == Environment.CurrentManagedThreadId;

    #endregion

    #region Chunk Management

    [Conditional("DEBUG")]
    private void AssertOwner() {
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);
        if (ownerThreadId != 0) {
            Debug.Assert(
                ownerThreadId == Environment.CurrentManagedThreadId,
                $"Dimension {Identifier} was changed outside its owner thread.");
        }
    }

    internal ExecutionDomainMailbox Mailbox => _mailbox;

    internal IReadOnlyCollection<Entity> GetEntities(int x, int z) {
        return _chunkEntities.TryGetValue(HashChunk(x, z), out List<Entity>? entities)
            ? entities
            : Array.Empty<Entity>();
    }

    internal bool ChunkLoaded(int x, int z) {
        return TryGetLoadedChunk(x, z, out _);
    }

    internal Entity[] GetEntitiesInRegionSnapshot(RegionCoordinate region) {
        HashSet<Entity> entities = [];
        int startX = region.X * RegionChunkSize;
        int startZ = region.Z * RegionChunkSize;
        for (int x = startX; x < startX + RegionChunkSize; x++) {
            for (int z = startZ; z < startZ + RegionChunkSize; z++) {
                if (_chunkEntities.TryGetValue(HashChunk(x, z), out List<Entity>? chunkEntities)) {
                    entities.UnionWith(chunkEntities);
                }
            }
        }

        return [.. entities];
    }

    internal Player.Player[] GetPlayersInRegionSnapshot(RegionCoordinate region) {
        HashSet<Player.Player> players = [];
        int startX = region.X * RegionChunkSize;
        int startZ = region.Z * RegionChunkSize;
        for (int x = startX; x < startX + RegionChunkSize; x++) {
            for (int z = startZ; z < startZ + RegionChunkSize; z++) {
                if (_playersByChunk.TryGetValue(HashChunk(x, z), out HashSet<Player.Player>? chunkPlayers)) {
                    players.UnionWith(chunkPlayers);
                }
            }
        }

        return [.. players];
    }

    internal static RegionCoordinate GetRegionCoordinate(
        int chunkX,
        int chunkZ,
        int regionChunkSize = DefaultRegionChunkSize) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(regionChunkSize);
        int regionX = chunkX / regionChunkSize;
        int regionZ = chunkZ / regionChunkSize;
        if (chunkX < 0 && chunkX % regionChunkSize != 0) {
            regionX--;
        }

        if (chunkZ < 0 && chunkZ % regionChunkSize != 0) {
            regionZ--;
        }

        return new(regionX, regionZ);
    }

    public bool TryGetLoadedChunk(int x, int z, out ChunkColumn? chunk) {
        lock (_chunkAccessLock) {
            return _chunks.TryGetValue(HashChunk(x, z), out chunk);
        }
    }

    public bool HasChunk(int x, int z) {
        long hash = HashChunk(x, z);
        return _chunks.ContainsKey(hash) ||
            World?.Persistence.ChunkPending(Type, x, z) == true ||
            _provider.HasChunk(Type, x, z);
    }

    [Obsolete("Use GetLoadedChunk for reads or RequestChunks for loading.")]
    public ChunkColumn? GetChunk(int x, int z) {
        return GetLoadedChunk(x, z);
    }

    public ChunkColumn? GetLoadedChunk(int x, int z) {
        return TryGetLoadedChunk(x, z, out ChunkColumn? chunk) ? chunk : null;
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
        World?.Persistence.WaitForChunk(Type, chunk.X, chunk.Z);
        lock (_chunkAccessLock) {
            long hash = HashChunk(chunk.X, chunk.Z);
            chunk.Simulated = _simulatedChunks.Contains(hash);
            _chunks[hash] = chunk;
            MaterializeEntities(chunk);
            if (chunk.Simulated) {
                RestoreBlockTicks(chunk);
            }
            SyncEntitiesToStorage(chunk);
            if (World is { } world) {
                world.Persistence.SaveChunk(chunk);
            }
            else {
                _provider.SaveChunk(chunk);
            }
        }
    }

    public int RequestChunks(ReadOnlySpan<(int X, int Z)> chunks, Action<ChunkColumn> ready) {
        if (_disposed) {
            return 0;
        }

        TaskScheduler? scheduler = World?.Server?.Scheduler;
        int accepted = 0;

        for (int i = 0; i < chunks.Length; i++) {
            (int x, int z) = chunks[i];
            long hash = HashChunk(x, z);

            if (_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                _chunkRequestCallbacks.Enqueue(new ChunkRequestCallback(chunk, ready));
                accepted++;
                continue;
            }

            Task saveCompletion = World?.Persistence.GetChunkSaveTask(Type, x, z) ?? Task.CompletedTask;

            lock (_chunkRequestLock) {
                if (_pendingChunkRequests.TryGetValue(hash, out PendingChunkRequest? request)) {
                    request.Callbacks.Add(ready);
                    accepted++;
                    continue;
                }

                if (_pendingChunkRequests.Count >= MaxPendingChunkRequests) {
                    break;
                }

                _pendingChunkRequests[hash] = new PendingChunkRequest(ready);
                _chunkViewers[hash] = _chunkViewers.TryGetValue(hash, out int count) ? count + 1 : 1;
                accepted++;
            }

            if (scheduler is null) {
                ChunkColumn? loaded = _provider.LoadChunk(Type, x, z);
                if (loaded is null) {
                    loaded = _generator.Generate(Type, x, z);
                    _generator.Populate(loaded);
                    loaded.Dirty = true;
                }
                if (!_mailbox.TryEnqueue(() => HandleChunkCompleted(hash, loaded))) {
                    CancelChunkRequest(hash);
                    return accepted - 1;
                }
            }
            else {
                ChunkGenerationTask task = new(
                    _provider,
                    _generator,
                    Type,
                    x,
                    z,
                    hash,
                    saveCompletion,
                    HandleChunkCompleted);
                task.CompletionMailbox = _mailbox;
                scheduler.Schedule(task);
            }
        }
        return accepted;
    }

    public bool RemoveChunk(int x, int z) {
        lock (_chunkAccessLock) {
            if (HashChunk(x, z) == HashChunk(WorldToChunk(SpawnPosition.X), WorldToChunk(SpawnPosition.Z))) {
                return false;
            }
        }

        World?.Persistence.WaitForChunk(Type, x, z);
        _provider.DeleteChunk(Type, x, z);
        long hash = HashChunk(x, z);

        lock (_chunkAccessLock) {
            return _chunks.Remove(hash);
        }
    }

    public void SaveDirtyChunks() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Dimension.SaveDirtyChunks") : default;

        if (World is { } persistenceWorld) {
            persistenceWorld.Persistence.SaveSpawnPosition(Type, SpawnPosition);
        }
        else {
            _provider.SaveSpawnPosition(Type, SpawnPosition);
        }

        foreach (ChunkColumn loadedChunk in _chunks.Values) {
            SyncBlockActorsToStorages(loadedChunk);
            SyncEntitiesToStorage(loadedChunk);
        }

        foreach (ChunkColumn chunk in _chunks.Values) {
            if (!chunk.Dirty) {
                continue;
            }

            try {
                if (World is not null) {
                    World.Persistence.SaveChunk(chunk);
                }
                else {
                    _provider.SaveChunk(chunk);
                }
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
            if (World is { } world) {
                world.Persistence.SaveChunk(chunk);
            }
            else {
                _provider.SaveChunk(chunk);
            }
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

    #endregion

    #region Blocks and Pathfinding

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
        PathfindingTask task = new(snapshot, start, target, completion, maxVisitedNodes, maxDistance) {
            CompletionMailbox = _mailbox
        };

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

    public BlockPermutation GetLoadedPermutationOrAir(int x, int y, int z, int layer = 0) {
        return TryGetLoadedPermutation(x, y, z, out BlockPermutation? permutation, layer) &&
            permutation is not null
            ? permutation
            : AirPermutation;
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true, bool broadcast = true) {
        AssertOwner();
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
                Position = position,
                BlockRuntimeId = (uint)permutation.NetworkId,
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
        if (!TryGetLoadedChunk(x >> 4, z >> 4, out ChunkColumn? chunk) || chunk is null) {
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
        if (!_chunks.TryGetValue(chunkHash, out ChunkColumn? chunk) || !chunk.Simulated || chunk.Empty) {
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
        if (chunk.Empty) {
            return;
        }

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
        ChunkColumn? chunk = GetLoadedChunk(x >> 4, z >> 4);
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
                        Position = new BlockPos() {
                            X = x,
                            Y = y,
                            Z = z,
                        },
                        RuntimeId = (uint)permutation.NetworkId,
                        EntityUniqueId = 0,
                        Message = 0,
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
                    Standards = entries.ToArray(),
                    Extras = []
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
        _mailbox.Complete();
        foreach (BlockTickTask task in _blockTicks.Values) {
            task.Cancel();
        }
        _blockTicks.Clear();
        FlushCompletedChunkRequests(int.MaxValue);
        if (World is not null) {
            World.Persistence.SaveSpawnPosition(Type, SpawnPosition);
        }
        else {
            _provider.SaveSpawnPosition(Type, SpawnPosition);
        }

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
        _playersByChunk.Clear();
        _playerChunks.Clear();
        Volatile.Write(ref _playersSnapshot, []);
    }

    #endregion

    #region Lifecycle and Ticking

    private void AddSimulationArea(long playerChunk, int simulationDistance) {
        int chunkX = (int)(playerChunk >> 32);
        int chunkZ = (int)playerChunk;

        for (int dx = -simulationDistance; dx <= simulationDistance; dx++) {
            for (int dz = -simulationDistance; dz <= simulationDistance; dz++) {
                long hash = HashChunk(chunkX + dx, chunkZ + dz);
                _simulationChunkReferences[hash] =
                    _simulationChunkReferences.TryGetValue(hash, out int count) ? count + 1 : 1;
            }
        }
    }

    private void RemoveSimulationArea(long playerChunk, int simulationDistance) {
        int chunkX = (int)(playerChunk >> 32);
        int chunkZ = (int)playerChunk;

        for (int dx = -simulationDistance; dx <= simulationDistance; dx++) {
            for (int dz = -simulationDistance; dz <= simulationDistance; dz++) {
                long hash = HashChunk(chunkX + dx, chunkZ + dz);
                if (!_simulationChunkReferences.TryGetValue(hash, out int count)) {
                    continue;
                }

                if (count <= 1) {
                    _simulationChunkReferences.Remove(hash);
                }
                else {
                    _simulationChunkReferences[hash] = count - 1;
                }
            }
        }
    }

    public void Tick(ulong currentTick, uint deltaTick) {
        if (Interlocked.CompareExchange(ref _ownerThreadId, Environment.CurrentManagedThreadId, 0) != 0) {
            return;
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try {
        using var __tick = Profiler.Enabled ? Profiler.BeginZone("Dimension.Tick") : default;

        _mailbox.Drain(MaxDomainCommandsPerTick, exception =>
            Logger.Warn($"Dimension mailbox command failed in {Identifier}: {exception}"));

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
        bool simulationDistanceChanged = false;
        int simulationDistance = 0;
        _simulationPlayerBuffer.Clear();

        if (World?.Server is Server server) {
            simulationDistance = Math.Clamp(server.Properties.SimulationDistance, 0, 120);

            foreach (Player.Player player in _players) {
                _simulationPlayerBuffer.Add(player);
                long hash = HashChunk(
                    WorldToChunk(player.Position.X),
                    WorldToChunk(player.Position.Z)
                );

                if (!_simulationPlayerChunks.TryGetValue(player, out long previous) || previous != hash) {
                    if (_simulationPlayerChunks.TryGetValue(player, out previous)) {
                        RemoveSimulationArea(previous, _simulationDistance < 0 ? simulationDistance : _simulationDistance);
                    }

                    _simulationPlayerChunks[player] = hash;
                    AddSimulationArea(hash, simulationDistance);
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
                Player.Player player = _simulationPlayerRemovalBuffer[i];
                if (_simulationPlayerChunks.Remove(player, out long previous)) {
                    RemoveSimulationArea(previous, _simulationDistance < 0 ? simulationDistance : _simulationDistance);
                }
            }

            simulationChanged = true;
        }

        if (_simulationDistance != simulationDistance) {
            _simulationDistance = simulationDistance;
            simulationDistanceChanged = true;
            simulationChanged = true;
        }

        if (simulationChanged) {
            using var simulationZone = Profiler.Enabled ? Profiler.BeginZone("Dimension.UpdateSimulation") : default;
            if (simulationDistanceChanged) {
                _simulationChunkReferences.Clear();
                foreach (long playerChunk in _simulationPlayerChunks.Values) {
                    AddSimulationArea(playerChunk, simulationDistance);
                }
            }

            _chunkSweepBuffer.Clear();
            foreach (long hash in _simulatedChunks) {
                if (!_simulationChunkReferences.ContainsKey(hash)) {
                    _chunkSweepBuffer.Add(hash);
                }
            }

            for (int i = 0; i < _chunkSweepBuffer.Count; i++) {
                long hash = _chunkSweepBuffer[i];
                if (_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                    chunk.Simulated = false;
                }
            }

            foreach (long hash in _simulationChunkReferences.Keys) {
                if (!_simulatedChunks.Contains(hash) &&
                    _chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                    chunk.Simulated = true;
                    RestoreBlockTicks(chunk);
                }
            }

            _simulatedChunks.Clear();
            _simulatedChunks.UnionWith(_simulationChunkReferences.Keys);
        }

        _tickEntityBuffer.Clear();
        foreach (long hash in _simulatedChunks) {
            if (_chunkEntities.TryGetValue(hash, out List<Entity>? entities)) {
                _tickEntityBuffer.AddRange(entities);
            }
        }

        bool regionMode = World?.Server is { } adaptiveServer &&
            (adaptiveServer.Properties.TickMode == TickMode.Region ||
             adaptiveServer.Properties.TickMode == TickMode.Adaptive &&
             adaptiveServer.WorkerPool.WorkerCount > 1 &&
             !TaskWorkerPool.WorkerThread);
        if (regionMode) {
            _tickRegionBuffers.Clear();
            for (int i = 0; i < _tickEntityBuffer.Count; i++) {
                Entity entity = _tickEntityBuffer[i];
                RegionCoordinate region = GetRegionCoordinate(
                    WorldToChunk(entity.Position.X),
                    WorldToChunk(entity.Position.Z),
                    RegionChunkSize);
                if (!_tickRegionBuffers.TryGetValue(region, out List<Entity>? regionEntities)) {
                    regionEntities = [];
                    _tickRegionBuffers[region] = regionEntities;
                }

                regionEntities.Add(entity);
            }
        }

        _tickingEntities = true;
        try {
            using (Profiler.Enabled ? Profiler.BeginZone("Dimension.TickEntities") : default) {
                if (regionMode) {
                    if (_tickRegionBuffers.Count > 1 &&
                        World?.Server?.WorkerPool is { } workerPool &&
                        workerPool.WorkerCount > 1) {
                        TickRegionsParallel(workerPool, currentTick, deltaTick);
                    }
                    else {
                        foreach (List<Entity> regionEntities in _tickRegionBuffers.Values) {
                            TickEntities(regionEntities, currentTick, deltaTick, _tickOwnedEntities, _pendingEntityRemoves);
                        }
                    }
                }
                else {
                    TickEntities(_tickEntityBuffer, currentTick, deltaTick, _tickOwnedEntities, _pendingEntityRemoves);
                }
            }
        }
        finally {
            _tickingEntities = false;
            for (int i = 0; i < _tickOwnedEntities.Count; i++) {
                _tickOwnedEntities[i].ReleaseTickOwner(this);
            }

            _tickOwnedEntities.Clear();
            if (regionMode) {
                foreach (List<Entity> regionEntities in _tickRegionBuffers.Values) {
                    regionEntities.Clear();
                }

                _tickRegionBuffers.Clear();
            }
        }
        FlushPendingEntityChanges();
        }
        finally {
            Volatile.Write(ref _tickWork, (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency);
            Volatile.Write(ref _ownerThreadId, 0);
        }
    }

    #endregion

    #region Networking and Entities

    private void TickRegionsParallel(TaskWorkerPool workerPool, ulong currentTick, uint deltaTick) {
        Volatile.Write(ref _parallelRegionTicking, 1);
        List<(RegionTickTask Task, ManualResetEventSlim Completed, List<Entity> Owned, HashSet<Entity> Removes)> tasks = [];

        try {
            foreach (List<Entity> regionEntities in _tickRegionBuffers.Values) {
                List<Entity> owned = [];
                HashSet<Entity> removes = [];
                ManualResetEventSlim completed = new();
                RegionTickTask task = new(
                    () => TickEntities(regionEntities, currentTick, deltaTick, owned, removes),
                    completed);

                if (workerPool.TryEnqueue(task)) {
                    tasks.Add((task, completed, owned, removes));
                }
                else {
                    completed.Dispose();
                    TickEntities(regionEntities, currentTick, deltaTick, owned, removes);
                    _tickOwnedEntities.AddRange(owned);
                    _pendingEntityRemoves.UnionWith(removes);
                }
            }

            for (int i = 0; i < tasks.Count; i++) {
                (RegionTickTask task, ManualResetEventSlim completed, List<Entity> owned, HashSet<Entity> removes) = tasks[i];
                completed.Wait();
                completed.Dispose();
                _tickOwnedEntities.AddRange(owned);
                _pendingEntityRemoves.UnionWith(removes);
                if (task.Error is not null) {
                    Logger.Warn($"Region tick failed in {Identifier}: {task.Error}");
                }
            }
        }
        finally {
            Volatile.Write(ref _parallelRegionTicking, 0);
            ApplyParallelRegionChanges();
        }
    }

    private void TickEntities(
        List<Entity> entities,
        ulong currentTick,
        uint deltaTick,
        List<Entity> ownedEntities,
        HashSet<Entity> pendingRemoves) {
        for (int i = 0; i < entities.Count; i++) {
            Entity entity = entities[i];
            if (!entity.TryClaimTickOwner(this)) {
                continue;
            }

            ownedEntities.Add(entity);
            if (entity.PendingDespawn || entity.Dimension != this) {
                pendingRemoves.Add(entity);
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
            if (entity.PendingDespawn || entity.Dimension != this) {
                pendingRemoves.Add(entity);
            }
        }
    }

    private void ApplyParallelRegionChanges() {
        while (_parallelEntityRemoves.TryDequeue(out (Entity Entity, bool Complete) removal)) {
            RemoveEntity(removal.Entity, removal.Complete);
        }

        foreach (Entity entity in _parallelEntityAdds.Keys) {
            if (_parallelEntityAdds.TryRemove(entity, out _)) {
                AddEntity(entity);
            }
        }

        foreach (Entity entity in _parallelStorageUpdates.Keys) {
            if (_parallelStorageUpdates.TryRemove(entity, out _)) {
                UpdateEntityStorage(entity);
            }
        }

        foreach (Entity entity in _parallelVisibilityUpdates.Keys) {
            if (_parallelVisibilityUpdates.TryRemove(entity, out _)) {
                UpdateEntityVisibility(entity);
            }
        }

        foreach (Entity entity in _parallelHiddenEntities.Keys) {
            if (_parallelHiddenEntities.TryRemove(entity, out _)) {
                HideEntity(entity);
            }
        }
    }

    public void Broadcast(Packet packet, BroadcastOptions? options = null) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Dimension.Broadcast") : default;
        if (World?.Server is not Server server) {
            return;
        }

        BroadcastOptions resolved = options ?? new BroadcastOptions();
        resolved.Center ??= GetPacketPosition(packet);
        float radiusSquared = resolved.Radius * resolved.Radius;

        foreach (Player.Player player in _players) {
            NetworkConnection? connection = player.Connection;
            if (connection is null) {
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
        if (Volatile.Read(ref _parallelRegionTicking) != 0 && !IsOwnerThread) {
            _parallelEntityAdds.TryAdd(entity, 0);
            return;
        }

        AssertOwner();
        if (_tickingEntities) {
            _pendingEntityRemoves.Remove(entity);
            _pendingEntityAdds.Add(entity);
            UpdateEntityStorage(entity);
            return;
        }

        _entities.Add(entity);
        if (entity is Player.Player player) {
            _players.Add(player);
            IndexPlayer(player);
            PublishPlayersSnapshot();
        }
        IndexEntity(entity);
        UpdateEntityStorage(entity);
        UpdateEntityVisibility(entity);
        Volatile.Write(ref _entitiesSnapshot, [.. _entities]);
        PublishCounts();
    }

    internal void RemoveEntity(Entity entity, bool complete = true) {
        if (Volatile.Read(ref _parallelRegionTicking) != 0 && !IsOwnerThread) {
            _parallelEntityRemoves.Enqueue((entity, complete));
            return;
        }

        AssertOwner();
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
        if (entity is Player.Player player) {
            _players.Remove(player);
            UnindexPlayer(player);
            PublishPlayersSnapshot();
        }

        Volatile.Write(ref _entitiesSnapshot, [.. _entities]);
        PublishCounts();
    }

    public void AddPlayer(Player.Player joining) {
        ulong tick = World is Tickable tickable ? tickable.TickValue : 0;
        PlayerChunkRenderingTrait? joiningRenderer = joining.GetTrait<PlayerChunkRenderingTrait>();
        _visibilityPlayerBuffer.Clear();
        CollectPlayersNear(joining.Location);
        foreach (Player.Player other in _visibilityPlayerBuffer) {
            if (ReferenceEquals(other, joining) || !other.IsAlive ||
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

    internal void UpdatePlayerVisibility(Player.Player moving, Vec3? previousPosition = null) {
        UpdatePlayerChunk(moving);
        PlayerChunkRenderingTrait? movingRenderer = moving.GetTrait<PlayerChunkRenderingTrait>();
        _visibilityPlayerBuffer.Clear();
        CollectPlayersNear(moving.Location);
        if (previousPosition is { } previous) {
            CollectPlayersNear(previous);
        }

        foreach (Player.Player other in _visibilityPlayerBuffer) {
            if (ReferenceEquals(other, moving)) {
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

    private void IndexPlayer(Player.Player player) {
        long hash = HashChunk(WorldToChunk(player.Position.X), WorldToChunk(player.Position.Z));
        _playerChunks[player] = hash;
        if (!_playersByChunk.TryGetValue(hash, out HashSet<Player.Player>? players)) {
            players = [];
            _playersByChunk[hash] = players;
        }

        players.Add(player);
    }

    private void UpdatePlayerChunk(Player.Player player) {
        long hash = HashChunk(WorldToChunk(player.Position.X), WorldToChunk(player.Position.Z));
        if (_playerChunks.TryGetValue(player, out long previous) && previous == hash) {
            return;
        }

        UnindexPlayer(player);
        IndexPlayer(player);
    }

    private void UnindexPlayer(Player.Player player) {
        if (!_playerChunks.Remove(player, out long hash) ||
            !_playersByChunk.TryGetValue(hash, out HashSet<Player.Player>? players)) {
            return;
        }

        players.Remove(player);
        if (players.Count == 0) {
            _playersByChunk.Remove(hash);
        }
    }

    private void CollectPlayersNear(Vec3 position) {
        int chunkX = WorldToChunk(position.X);
        int chunkZ = WorldToChunk(position.Z);
        for (int dx = -4; dx <= 4; dx++) {
            for (int dz = -4; dz <= 4; dz++) {
                if (_playersByChunk.TryGetValue(HashChunk(chunkX + dx, chunkZ + dz), out HashSet<Player.Player>? players)) {
                    _visibilityPlayerBuffer.UnionWith(players);
                }
            }
        }
    }

    internal Player.Player[] GetPlayersNearSnapshot(Vec3 position) {
        _visibilityPlayerBuffer.Clear();
        CollectPlayersNear(position);
        return [.. _visibilityPlayerBuffer];
    }

    private ChunkColumn? GetOrLoadChunk(int x, int z) {
        lock (_chunkAccessLock) {
            long hash = HashChunk(x, z);
            if (_chunks.TryGetValue(hash, out ChunkColumn? chunk)) {
                return chunk;
            }

            ChunkColumn? pending = World?.Persistence.GetPendingChunk(Type, x, z);
            if (pending is not null) {
                pending.Simulated = _simulatedChunks.Contains(hash);
                _chunks[hash] = pending;
                MaterializeEntities(pending);
                if (pending.Simulated) {
                    RestoreBlockTicks(pending);
                }

                return pending;
            }

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
                if (entity is Player.Player player) {
                    _players.Remove(player);
                    UnindexPlayer(player);
                }
            }

            _pendingEntityRemoves.Clear();
        }

        if (_pendingEntityAdds.Count > 0) {
            foreach (Entity entity in _pendingEntityAdds) {
                _entities.Add(entity);
                if (entity is Player.Player player) {
                    _players.Add(player);
                    IndexPlayer(player);
                }
                IndexEntity(entity);
                UpdateEntityVisibility(entity);
            }

            _pendingEntityAdds.Clear();
        }

        PublishPlayersSnapshot();
    }

    private void PublishCounts() {
        Volatile.Write(ref _activePlayerCount, _players.Count);
        Volatile.Write(ref _activeEntityCount, _entities.Count);
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
        if (Volatile.Read(ref _parallelRegionTicking) != 0 && !IsOwnerThread) {
            _parallelStorageUpdates.TryAdd(entity, 0);
            return;
        }

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

        if (!_chunkEntities.TryGetValue(hash, out List<Entity>? entities)) {
            entities = [];
            _chunkEntities[hash] = entities;
        }

        int index = entities.Count;
        entities.Add(entity);
        _entityChunkIndexes[entity] = new EntityChunkIndex(hash, index);
    }

    private bool UpdateEntityIndex(Entity entity) {
        long hash = HashChunk(WorldToChunk(entity.Position.X), WorldToChunk(entity.Position.Z));
        if (_entityChunkIndexes.TryGetValue(entity, out EntityChunkIndex previous)) {
            if (previous.Hash == hash) {
                return false;
            }

            RemoveEntityFromChunk(previous);
        }

        if (!_chunkEntities.TryGetValue(hash, out List<Entity>? entities)) {
            entities = [];
            _chunkEntities[hash] = entities;
        }

        int index = entities.Count;
        entities.Add(entity);
        _entityChunkIndexes[entity] = new EntityChunkIndex(hash, index);
        return true;
    }

    private void UnindexEntity(Entity entity) {
        if (!_entityChunkIndexes.Remove(entity, out EntityChunkIndex index)) {
            return;
        }

        RemoveEntityFromChunk(index);
    }

    private void RemoveEntityFromChunk(EntityChunkIndex index) {
        if (!_chunkEntities.TryGetValue(index.Hash, out List<Entity>? entities)) {
            return;
        }

        int lastIndex = entities.Count - 1;
        if (index.Index < lastIndex) {
            Entity moved = entities[lastIndex];
            entities[index.Index] = moved;
            _entityChunkIndexes[moved] = new EntityChunkIndex(index.Hash, index.Index);
        }

        entities.RemoveAt(lastIndex);
        if (entities.Count == 0) {
            _chunkEntities.Remove(index.Hash);
        }
    }

    internal void UpdateEntityVisibility(Entity entity) {
        if (Volatile.Read(ref _parallelRegionTicking) != 0 && !IsOwnerThread) {
            _parallelVisibilityUpdates.TryAdd(entity, 0);
            return;
        }

        _visibilityPlayerBuffer.Clear();
        CollectPlayersNear(entity.Position);
        foreach (Player.Player player in _visibilityPlayerBuffer) {
            player.GetTrait<PlayerChunkRenderingTrait>()?.UpdateVisibleEntity(entity);
        }
    }

    internal void HideEntity(Entity entity) {
        if (Volatile.Read(ref _parallelRegionTicking) != 0 && !IsOwnerThread) {
            _parallelHiddenEntities.TryAdd(entity, 0);
            return;
        }

        _visibilityPlayerBuffer.Clear();
        CollectPlayersNear(entity.Position);
        foreach (Player.Player player in _visibilityPlayerBuffer) {
            player.GetTrait<PlayerChunkRenderingTrait>()?.HideVisibleEntity(entity);
        }
    }

    #endregion

    #region Entity Storage

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
                return ToVec3(updateBlock.Position.X, updateBlock.Position.Y, updateBlock.Position.Z);

            case BlockActorDataPacket blockActor:
                return ToVec3(blockActor.Position.X, blockActor.Position.Y, blockActor.Position.Z);

            case LevelEventPacket levelEvent:
                return levelEvent.Position;

            case BlockEventPacket blockEvent:
                return ToVec3(blockEvent.Position.X, blockEvent.Position.Y, blockEvent.Position.Z);

            case LevelSoundEventPacket levelSoundEvent:
                return levelSoundEvent.Position;

            case MovePlayerPacket movePlayer:
                return movePlayer.Position;

            case MoveActorDeltaPacket moveActorDelta:
                return new Vec3() {
                    X = moveActorDelta.PositionX ?? 0,
                    Y = moveActorDelta.PositionY ?? 0,
                    Z = moveActorDelta.PositionZ ?? 0,
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

    #endregion

    #region Persistence

    private void CancelChunkRequest(long hash) {
        lock (_chunkRequestLock) {
            _pendingChunkRequests.Remove(hash);
            if (!_chunkViewers.TryGetValue(hash, out int viewers)) {
                return;
            }

            if (viewers <= 1) {
                _chunkViewers.Remove(hash);
            }
            else {
                _chunkViewers[hash] = viewers - 1;
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
                chunk.Dirty = false;
                if (World is { } world) {
                    world.Persistence.SaveChunk(chunk);
                }
                else {
                    _provider.SaveChunk(chunk);
    }

    #endregion
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

    private readonly record struct EntityChunkIndex(long Hash, int Index);
}

[Flags]
public enum UpdateBlockFlagsType : uint {
    None = 0,
    Neighbors = 1,
    Network = 2,
    NoGraphic = 4,
    Priority = 8
}
