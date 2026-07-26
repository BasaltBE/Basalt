namespace Basalt.Core.Worlds.Dimensions;

using System.Collections.Concurrent;
using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using ChunkColumn = Chunk.Chunk;

using Entity = Entities.Entity;

public sealed class Dimension : IDisposable {
    private const int CompletedChunkLimit = 128;

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
    private readonly Dictionary<long, int> _chunkViewers;
    private readonly HashSet<long> _pendingUnloads = [];
    private readonly HashSet<Entity> _entities;
    private readonly Dictionary<Entity, long> _entityChunks = [];
    private readonly List<long> _chunkSweepBuffer = [];
    private readonly HashSet<Entity> _pendingEntityAdds = [];
    private readonly HashSet<Entity> _pendingEntityRemoves = [];
    private readonly Dictionary<Player.Player, long> _simulationPlayerChunks = [];
    private readonly HashSet<Player.Player> _simulationPlayerBuffer = [];
    private readonly List<Player.Player> _simulationPlayerRemovalBuffer = [];
    private readonly HashSet<long> _simulatedChunks = [];
    private readonly HashSet<long> _simulationChunkBuffer = [];
    private readonly Lock _chunkRequestLock = new();
    private readonly Dictionary<long, PendingChunkRequest> _pendingChunkRequests = [];
    private readonly ConcurrentQueue<ChunkRequestCallback> _chunkRequestCallbacks = new();
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private ChunkColumn[]? _autoSaveChunks;
    private int _autoSaveIndex;
    private int _simulationDistance = -1;
    private bool _tickingEntities;
    private bool _disposed;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public Vec3f SpawnPosition { get; set; } = new(0, 80, 0);
    public World? World { get; internal set; }
    public DimensionGameRules Gamerules { get; } = new();

    public Dimension(string identifier, DimensionType type, WorldProvider provider, Generator? generator = null) {
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
    public IReadOnlyCollection<Entity> Entities => _entities;

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
        return chunk;
    }

    public void SetChunk(ChunkColumn chunk) {
        long hash = HashChunk(chunk.X, chunk.Z);
        World?.Persistence.WaitForChunk(Type, chunk.X, chunk.Z);
        chunk.Simulated = _simulatedChunks.Contains(hash);
        _chunks[hash] = chunk;
        MaterializeEntities(chunk);
        SyncEntitiesToStorage(chunk);
        _provider.SaveChunk(chunk);
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
        World?.Persistence.WaitForChunk(Type, x, z);
        _provider.DeleteChunk(Type, x, z);
        long hash = HashChunk(x, z);
        return _chunks.Remove(hash);
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
        long hash = HashChunk(x, z);
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

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true, bool broadcast = true) {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);

        BlockPermutation previous = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
        bool wasFluid = previous.Type.Liquid && !permutation.Type.Liquid;

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
                NetworkBlockId = (uint)permutation.NetworkId,
                Flags = UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network,
                Layer = (UpdateBlockLayerType)layer
            },
            new BroadcastOptions {
                Radius = World?.Server?.Properties.MaxViewDistance * 16 ?? 256,
            });
        }

        if (wasFluid) {
            Basalt.Core.Blocks.Traits.FluidKind? kind = Basalt.Core.Blocks.Traits.FluidTrait.GetFluidKind(previous);
            if (kind.HasValue) {
                Basalt.Core.Blocks.Traits.FluidTrait.NotifyFluidNeighbors(kind.Value, this, position);
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
        Dictionary<(int cx, int cy, int cz), List<BlockChangeEntry>> subChunkEntries = [];

        for (int x = minX; x <= maxX; x++) {
            for (int z = minZ; z <= maxZ; z++) {
                for (int y = minY; y <= maxY; y++) {
                    SetPermutation(x, y, z, permutation, broadcast: false);
                    filled++;

                    int cx = x >> 4;
                    int cy = y >> 4;
                    int cz = z >> 4;
                    var key = (cx, cy, cz);

                    if (!subChunkEntries.TryGetValue(key, out List<BlockChangeEntry>? entries)) {
                        entries = [];
                        subChunkEntries[key] = entries;
                    }

                    entries.Add(new BlockChangeEntry {
                        Position = new BlockPos { X = x, Y = y, Z = z },
                        BlockRuntimeId = (uint)permutation.NetworkId,
                        Flags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network),
                        SyncedUpdateEntityUniqueId = 0,
                        SyncedUpdateType = 0
                    });
                }
            }
        }

        float broadcastRadius = World?.Server?.Properties.MaxViewDistance * 16 ?? 256;
        foreach (((int scx, int scy, int scz), List<BlockChangeEntry> entries) in subChunkEntries) {
            Broadcast(new UpdateSubChunkBlocksPacket {
                SubChunkX = scx,
                SubChunkY = scy,
                SubChunkZ = scz,
                Blocks = entries
            }, new BroadcastOptions {
                Radius = broadcastRadius,
                Center = new Vec3f {
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
                }
            }

            _simulatedChunks.Clear();
            _simulatedChunks.UnionWith(_simulationChunkBuffer);
        }

        _tickingEntities = true;
        using (Profiler.Enabled ? Profiler.BeginZone("Dimension.TickEntities") : default) {
            foreach (Entity entity in _entities) {
                if (entity.PendingDespawn || entity.Dimension != this) {
                    _pendingEntityRemoves.Add(entity);
                    continue;
                }

                if (entity is Player.Player) {
                    entity.Tick(currentTick, deltaTick);
                    continue;
                }

                if (!EntityInSimulatedChunk(entity)) {
                    continue;
                }

                entity.Tick(currentTick, deltaTick);
            }
        }
        _tickingEntities = false;
        FlushPendingEntityChanges();
    }

    public void Broadcast(DataPacket packet, BroadcastOptions? options = null) {
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

            if (resolved.Center.HasValue) {
                Vec3f playerPosition = player.Position;
                Vec3f centerPosition = resolved.Center.Value;
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

    internal void AddEntity(Entity entity) {
        if (_tickingEntities) {
            _pendingEntityRemoves.Remove(entity);
            _pendingEntityAdds.Add(entity);
            UpdateEntityStorage(entity);
            return;
        }

        _entities.Add(entity);
        UpdateEntityStorage(entity);
    }

    internal void RemoveEntity(Entity entity, bool complete = true) {
        if (_tickingEntities) {
            _pendingEntityAdds.Remove(entity);
            _pendingEntityRemoves.Add(entity);
            return;
        }

        RemoveEntityStorage(entity);
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
        int joiningChunkX = WorldToChunk(joining.Location.X);
        int joiningChunkZ = WorldToChunk(joining.Location.Z);
        int viewDistance = server.Properties.MaxViewDistance;

        foreach ((_, Player.Player other) in server.Players) {
            if (ReferenceEquals(other, joining) || other.Dimension != this) {
                continue;
            }

            int otherChunkX = WorldToChunk(other.Location.X);
            int otherChunkZ = WorldToChunk(other.Location.Z);

            bool otherInRange = InViewRange(joiningChunkX, joiningChunkZ, otherChunkX, otherChunkZ, viewDistance);
            other.SpawnTo(joining, tick, otherInRange ? other.Location : new Vec3f());

            bool joiningInRange = InViewRange(otherChunkX, otherChunkZ, joiningChunkX, joiningChunkZ, viewDistance);
            joining.SpawnTo(other, tick, joiningInRange ? joining.Location : new Vec3f());
        }
    }

    public void RemovePlayer(Player.Player leaving) {
        Broadcast(new RemoveActorPacket {
            EntityUniqueId = leaving.UniqueId
        }, new BroadcastOptions { Except = [leaving] });
    }

    private static bool InViewRange(int viewerChunkX, int viewerChunkZ, int targetChunkX, int targetChunkZ, int viewDistance) {
        int dx = targetChunkX - viewerChunkX;
        int dz = targetChunkZ - viewerChunkZ;
        return Math.Max(Math.Abs(dx), Math.Abs(dz)) <= viewDistance;
    }

    private static long HashChunk(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }

    private bool EntityInSimulatedChunk(Entity entity) {
        int chunkX = WorldToChunk(entity.Position.X);
        int chunkZ = WorldToChunk(entity.Position.Z);
        return _chunks.TryGetValue(HashChunk(chunkX, chunkZ), out ChunkColumn? chunk) && chunk.Simulated;
    }

    private static int WorldToChunk(float coordinate) {
        return (int)MathF.Floor(coordinate) >> 4;
    }

    private ChunkColumn? GetOrLoadChunk(int x, int z) {
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
        }

        return chunk;
    }

    private static int GetChunkLocal(int value) {
        return value & 0xF;
    }

    private void FlushPendingEntityChanges() {
        if (_pendingEntityRemoves.Count > 0) {
            foreach (Entity entity in _pendingEntityRemoves) {
                RemoveEntityStorage(entity);
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
        if (entity is Player.Player || entity.Dimension != this) {
            return;
        }

        if (entity.PendingDespawn) {
            RemoveEntityStorage(entity);
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
        if (entity is Player.Player) {
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

    private void SyncEntitiesToStorage(ChunkColumn chunk) {
        foreach (Entity entity in _entities) {
            if (entity is Player.Player) {
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
                    entity = new Entity(identifier);
                }

                if (entity is null) {
                    continue;
                }

                entity.RestoreUniqueId(stored.Key);
                entity.Read(tag);
                entity.Spawn(this, new EntitySpawnOptions(InitialSpawn: true));
            }
            catch (Exception exception) {
                Logger.Warn($"Failed materializing entity {stored.Key} in chunk {chunk.X},{chunk.Z}: {exception.Message}");
            }
        }
    }

    private void UnloadEntities(ChunkColumn chunk) {
        List<Entity> unloaded = [];
        foreach (Entity entity in _entities) {
            if (entity is Player.Player ||
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
            entity.CompleteDespawn();
        }
    }

    internal static string GetBlockActorId(string blockIdentifier) {
        return BlockActorIds.TryGetValue(blockIdentifier, out string? value) ? value : blockIdentifier;
    }

    private static Vec3f? GetPacketPosition(DataPacket packet) {
        switch (packet) {
            case UpdateBlockPacket updateBlock:
                return ToVec3f(updateBlock.Position.X, updateBlock.Position.Y, updateBlock.Position.Z);

            case BlockActorDataPacket blockActor:
                return ToVec3f(blockActor.Position.X, blockActor.Position.Y, blockActor.Position.Z);

            case LevelEventPacket levelEvent:
                return levelEvent.Position;

            case BlockEventPacket blockEvent:
                return ToVec3f(blockEvent.Position.X, blockEvent.Position.Y, blockEvent.Position.Z);

            case LevelSoundEventPacket levelSoundEvent:
                return levelSoundEvent.Position;

            case MovePlayerPacket movePlayer:
                return movePlayer.Position;

            default:
                return null;
        }
    }

    private static Vec3f ToVec3f(float x, float y, float z) {
        return new Vec3f { X = x, Y = y, Z = z };
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







