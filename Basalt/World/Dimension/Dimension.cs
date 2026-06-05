namespace Basalt.Core.World.Dimension;

using System.Collections.Concurrent;
using Basalt.Core.Block;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Basalt.Core.World.Dimension.Generation;
using Basalt.Core.World.Dimension.Provider;
using ChunkColumn = Basalt.Core.World.Dimension.Chunk.Chunk;

using Entity = Basalt.Core.Entity.Entity;

public sealed class Dimension : IDisposable
{
    private const int CompletedChunkLimit = 128;
    private static readonly int ChunkWorkerLimit = Math.Clamp(Environment.ProcessorCount - 1, 1, 4);

    /// <summary>
    /// A mapping of block actors with containers
    /// </summary>
    private static readonly Dictionary<string, string> BlockActorIds = new()
    {
        ["minecraft:barrel"] = "Barrel",
        ["minecraft:chest"] = "Chest",
        ["minecraft:trapped_chest"] = "Chest"
    };

    /// <summary>
    /// A list of chunks in the dimension
    /// </summary>
    private readonly Dictionary<long, ChunkColumn> _chunks;

    /// <summary>
    /// A list of chunk viewers
    /// </summary>
    private readonly Dictionary<long, int> _chunkViewers;

    /// <summary>
    /// A list of entities
    /// </summary>
    private readonly HashSet<Entity> _entities;

    /// <summary>
    ///  A list of chunks to sweep
    /// </summary>
    private readonly List<long> _chunkSweepBuffer = [];

    private readonly HashSet<Entity> _pendingEntityAdds = [];
    private readonly HashSet<Entity> _pendingEntityRemoves = [];
    private readonly Lock _chunkRequestLock = new();
    private readonly Dictionary<long, PendingChunkRequest> _pendingChunkRequests = [];
    private readonly ConcurrentQueue<long> _chunkRequests = new();
    private readonly ConcurrentQueue<CompletedChunkRequest> _completedChunkRequests = new();
    private readonly ConcurrentQueue<ChunkRequestCallback> _chunkRequestCallbacks = new();
    private readonly SemaphoreSlim _chunkRequestSignal = new(0);
    private readonly CancellationTokenSource _chunkRequestCancel = new();
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private bool _tickingEntities;
    private bool _disposed;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public global::Basalt.Core.World.World? World { get; internal set; }
    public global::Basalt.Core.World.DimensionGameRules Gamerules { get; } = new();

    public Dimension(string identifier, DimensionType type, WorldProvider provider, Generator? generator = null)
    {
        Identifier = identifier;
        Type = type;
        _chunks = [];
        _chunkViewers = [];
        _entities = [];
        _provider = provider;
        _generator = generator ?? new VoidGenerator();

        for (int i = 0; i < ChunkWorkerLimit; i++)
        {
            _ = Task.Run(ChunkRequestWorker);
        }
    }

    public int ChunkCount => _chunks.Count;
    public int ChunkViewerCount => _chunkViewers.Count;
    public IReadOnlyCollection<Entity> Entities => _entities;

    public bool HasChunk(int x, int z)
    {
        long hash = HashChunk(x, z);
        return _chunks.ContainsKey(hash) || _provider.HasChunk(Type, x, z);
    }

    public ChunkColumn? GetChunk(int x, int z)
    {
        return GetOrLoadChunk(x, z);
    }

    public ChunkColumn GetOrCreateChunk(int x, int z)
    {
        ChunkColumn? chunk = GetOrLoadChunk(x, z);
        if (chunk is not null)
        {
            return chunk;
        }

        long hash = HashChunk(x, z);
        chunk = _generator.Generate(Type, x, z);
        _generator.Populate(chunk);
        chunk.Dirty = true;
        _chunks[hash] = chunk;
        return chunk;
    }

    public void SetChunk(ChunkColumn chunk)
    {
        _chunks[HashChunk(chunk.X, chunk.Z)] = chunk;
        _provider.SaveChunk(chunk);
    }

    public void RequestChunks(ReadOnlySpan<(int X, int Z)> chunks, Action<ChunkColumn> ready)
    {
        if (_disposed)
        {
            return;
        }

        for (int i = 0; i < chunks.Length; i++)
        {
            (int x, int z) = chunks[i];
            long hash = HashChunk(x, z);

            if (_chunks.TryGetValue(hash, out ChunkColumn? chunk))
            {
                _chunkRequestCallbacks.Enqueue(new ChunkRequestCallback(chunk, ready));
                continue;
            }

            lock (_chunkRequestLock)
            {
                if (_pendingChunkRequests.TryGetValue(hash, out PendingChunkRequest? request))
                {
                    request.Callbacks.Add(ready);
                    continue;
                }

                _pendingChunkRequests[hash] = new PendingChunkRequest(ready);
            }

            _chunkRequests.Enqueue(hash);
            _chunkRequestSignal.Release();
        }
    }

    public bool RemoveChunk(int x, int z)
    {
        _provider.DeleteChunk(Type, x, z);
        long hash = HashChunk(x, z);
        if (!_chunks.TryGetValue(hash, out ChunkColumn? chunk))
        {
            return false;
        }

        chunk.ReleaseMemory();
        return _chunks.Remove(hash);
    }

    public void SaveDirtyChunks()
    {
        foreach (ChunkColumn loadedChunk in _chunks.Values)
        {
            SyncBlockActorsToStorages(loadedChunk);
        }

        foreach (ChunkColumn chunk in _chunks.Values)
        {
            if (!chunk.Dirty)
            {
                continue;
            }

            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }
    }

    public bool SaveChunk(int x, int z)
    {
        if (!_chunks.TryGetValue(HashChunk(x, z), out ChunkColumn? chunk))
        {
            return false;
        }

        SyncBlockActorsToStorages(chunk);
        _provider.SaveChunk(chunk);
        chunk.Dirty = false;
        return true;
    }

    public bool UnloadChunk(int x, int z, bool save = true)
    {
        long hash = HashChunk(x, z);
        if (!_chunks.TryGetValue(hash, out ChunkColumn? chunk))
        {
            return false;
        }

        if (save && chunk.Dirty)
        {
            SyncBlockActorsToStorages(chunk);
            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }

        chunk.ReleaseMemory();
        return _chunks.Remove(hash);
    }

    public void AddChunkViewer(int x, int z)
    {
        long hash = HashChunk(x, z);
        _chunkViewers[hash] = _chunkViewers.TryGetValue(hash, out int count) ? count + 1 : 1;
    }

    public bool RemoveChunkViewer(int x, int z)
    {
        long hash = HashChunk(x, z);
        if (!_chunkViewers.TryGetValue(hash, out int count))
        {
            return false;
        }

        if (count <= 1)
        {
            _chunkViewers.Remove(hash);
            return true;
        }

        _chunkViewers[hash] = count - 1;
        return true;
    }

    public bool HasChunkViewers(int x, int z)
    {
        return _chunkViewers.ContainsKey(HashChunk(x, z));
    }

    public int UnloadUnviewedChunks(int limit, bool save = true)
    {
        if (_chunks.Count == 0 || limit <= 0)
        {
            return 0;
        }

        int unloaded = 0;
        _chunkSweepBuffer.Clear();
        _chunkSweepBuffer.EnsureCapacity(_chunks.Count);

        foreach (long hash in _chunks.Keys)
        {
            _chunkSweepBuffer.Add(hash);
        }

        for (int i = 0; i < _chunkSweepBuffer.Count && unloaded < limit; i++)
        {
            long hash = _chunkSweepBuffer[i];
            if (_chunkViewers.ContainsKey(hash))
            {
                continue;
            }

            int x = (int)(hash >> 32);
            int z = (int)hash;
            if (UnloadChunk(x, z, save))
            {
                unloaded++;
            }
        }

        return unloaded;
    }

    public IEnumerable<ChunkColumn> GetChunks()
    {
        return _chunks.Values;
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), permutation, layer, dirty);

        BlockPos position = new() { X = x, Y = y, Z = z };
        if (permutation.Type.Traits.Count > 0)
        {
            global::Basalt.Core.Block.Block? block = chunk.GetBlockActor(position);
            if (block is null)
            {
                block = new global::Basalt.Core.Block.Block(permutation);
                chunk.SetBlockActor(position, block);
            }
            else
            {
                block.SetPermutation(permutation);
            }

            BlockLevelStorage storage = GetOrCreateBlockStorage(chunk, position, permutation.Type.Identifier);
            chunk.SetBlockStorage(position, storage, dirty);
        }
        else
        {
            chunk.SetBlockActor(position, null);
            chunk.SetBlockStorage(position, null, dirty);
        }
    }

    public global::Basalt.Core.Block.Block? GetBlock(int x, int y, int z)
    {
        ChunkColumn? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null)
        {
            return null;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };
        global::Basalt.Core.Block.Block? block = chunk.GetBlockActor(position);
        if (block is not null)
        {
            return block;
        }

        BlockPermutation perm = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z));
        if (perm.Type.Traits.Count > 0)
        {
            block = new global::Basalt.Core.Block.Block(perm);
            BlockLevelStorage? storage = chunk.GetBlockStorage(position);
            if (storage is not null)
            {
                block.ReadTraits(storage);
            }

            chunk.SetBlockActor(position, block);
            return block;
        }

        return null;
    }

    public void SetBlock(int x, int y, int z, global::Basalt.Core.Block.Block block)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBlockActor(new BlockPos { X = x, Y = y, Z = z }, block);
    }

    public void RemoveBlock(int x, int y, int z)
    {
        ChunkColumn? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null)
        {
            return;
        }

        chunk.SetBlockActor(new BlockPos { X = x, Y = y, Z = z }, null);
    }

    public int GetBiome(int x, int y, int z)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetBiome(GetChunkLocal(x), y, GetChunkLocal(z));
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBiome(GetChunkLocal(x), y, GetChunkLocal(z), biomeId, dirty);
    }

    public void Dispose()
    {
        _disposed = true;
        _chunkRequestCancel.Cancel();
        _chunkRequestSignal.Release(ChunkWorkerLimit);
        FlushCompletedChunkRequests(int.MaxValue);
        SaveDirtyChunks();
    }

    public void Tick(ulong currentTick, uint deltaTick)
    {
        FlushCompletedChunkRequests(CompletedChunkLimit);

        if (currentTick % 20 == 0 && _chunks.Count > 0)
        {
            int unloadLimit = Math.Min(Math.Max(_chunks.Count / 8, 32), 256);
            _ = UnloadUnviewedChunks(unloadLimit, save: true);
        }

        if (_entities.Count == 0)
        {
            return;
        }

        foreach (ChunkColumn chunk in _chunks.Values)
        {
            chunk.Simulated = false;
        }

        if (World?.Server is global::Basalt.Core.Server server)
        {
            int simulationDistance = Math.Clamp(server.Properties.SimulationDistance, 0, 120);
            foreach ((_, var player) in server.Players)
            {
                if (player.Dimension != this)
                {
                    continue;
                }

                int currentChunkX = WorldToChunk(player.Position.X);
                int currentChunkZ = WorldToChunk(player.Position.Z);

                for (int dx = -simulationDistance; dx <= simulationDistance; dx++)
                {
                    for (int dz = -simulationDistance; dz <= simulationDistance; dz++)
                    {
                        int x = currentChunkX + dx;
                        int z = currentChunkZ + dz;
                        ChunkColumn? chunk = GetChunk(x, z);
                        if (chunk is not null)
                        {
                            chunk.Simulated = true;
                        }
                    }
                }
            }
        }

        _tickingEntities = true;
        foreach (Entity entity in _entities)
        {
            if (entity.PendingDespawn || entity.Dimension != this)
            {
                _pendingEntityRemoves.Add(entity);
                continue;
            }

            if (entity is global::Basalt.Core.Player.Player)
            {
                entity.Tick(currentTick, deltaTick);
                continue;
            }

            if (!EntityInSimulatedChunk(entity))
            {
                continue;
            }

            entity.Tick(currentTick, deltaTick);
        }

        _tickingEntities = false;
        FlushPendingEntityChanges();
    }

    public void Broadcast(DataPacket packet, BroadcastOptions? options = null)
    {
        if (World?.Server is not global::Basalt.Core.Server server)
        {
            return;
        }

        BroadcastOptions resolved = options ?? new BroadcastOptions();
        resolved.Center ??= GetPacketPosition(packet);
        float radiusSquared = resolved.Radius * resolved.Radius;

        foreach ((var connection, var player) in server.Players)
        {
            if (player.Dimension != this)
            {
                continue;
            }

            if (resolved.Except is not null && resolved.Except.Contains(player))
            {
                continue;
            }

            if (resolved.Center.HasValue)
            {
                Vec3f playerPosition = player.Position;
                Vec3f centerPosition = resolved.Center.Value;
                float dx = playerPosition.X - centerPosition.X;
                float dy = playerPosition.Y - centerPosition.Y;
                float dz = playerPosition.Z - centerPosition.Z;
                float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }
            }

            server.Network.SendPacket(connection, packet);
        }
    }

    internal void AddEntity(Entity entity)
    {
        if (_tickingEntities)
        {
            _pendingEntityRemoves.Remove(entity);
            _pendingEntityAdds.Add(entity);
            return;
        }

        _entities.Add(entity);
    }

    internal void RemoveEntity(Entity entity, bool complete = true)
    {
        if (_tickingEntities)
        {
            _pendingEntityAdds.Remove(entity);
            _pendingEntityRemoves.Add(entity);
            return;
        }

        if (complete)
        {
            entity.CompleteDespawn();
        }
        _entities.Remove(entity);
    }

    private static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }

    private bool EntityInSimulatedChunk(Entity entity)
    {
        int chunkX = WorldToChunk(entity.Position.X);
        int chunkZ = WorldToChunk(entity.Position.Z);
        return _chunks.TryGetValue(HashChunk(chunkX, chunkZ), out ChunkColumn? chunk) && chunk.Simulated;
    }

    private static int WorldToChunk(float coordinate)
    {
        return (int)MathF.Floor(coordinate) >> 4;
    }

    private ChunkColumn? GetOrLoadChunk(int x, int z)
    {
        long hash = HashChunk(x, z);
        if (_chunks.TryGetValue(hash, out ChunkColumn? chunk))
        {
            return chunk;
        }

        chunk = _provider.LoadChunk(Type, x, z);
        if (chunk is not null)
        {
            _chunks[hash] = chunk;
        }

        return chunk;
    }

    private static int GetChunkLocal(int value)
    {
        return value & 0xF;
    }

    private void FlushPendingEntityChanges()
    {
        if (_pendingEntityRemoves.Count > 0)
        {
            foreach (Entity entity in _pendingEntityRemoves)
            {
                if (entity.Dimension == this)
                {
                    entity.CompleteDespawn();
                }
                _entities.Remove(entity);
            }

            _pendingEntityRemoves.Clear();
        }

        if (_pendingEntityAdds.Count > 0)
        {
            foreach (Entity entity in _pendingEntityAdds)
            {
                _entities.Add(entity);
            }

            _pendingEntityAdds.Clear();
        }
    }

    private static BlockLevelStorage GetOrCreateBlockStorage(ChunkColumn chunk, BlockPos position, string blockIdentifier)
    {
        BlockLevelStorage? storage = chunk.GetBlockStorage(position);
        if (storage is not null)
        {
            return storage;
        }

        storage = new BlockLevelStorage(chunk);
        storage.SetPosition(position);
        storage.Set("id", new StringTag { Name = "id", Value = GetBlockActorId(blockIdentifier) });
        storage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
        return storage;
    }

    private static void SyncBlockActorsToStorages(ChunkColumn chunk)
    {
        foreach (KeyValuePair<(int X, int Y, int Z), global::Basalt.Core.Block.Block> actorEntry in chunk.GetAllBlockActors())
        {
            BlockPos position = new()
            {
                X = actorEntry.Key.X,
                Y = actorEntry.Key.Y,
                Z = actorEntry.Key.Z
            };

            BlockLevelStorage storage = GetOrCreateBlockStorage(chunk, position, actorEntry.Value.Type.Identifier);
            actorEntry.Value.WriteTraits(storage);
            chunk.SetBlockStorage(position, storage, dirty: true);
        }
    }

    internal static string GetBlockActorId(string blockIdentifier)
    {
        return BlockActorIds.TryGetValue(blockIdentifier, out string? value) ? value : blockIdentifier;
    }

    private static Vec3f? GetPacketPosition(DataPacket packet)
    {
        switch (packet)
        {
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

    private static Vec3f ToVec3f(float x, float y, float z)
    {
        return new Vec3f { X = x, Y = y, Z = z };
    }

    private void FlushCompletedChunkRequests(int limit)
    {
        int completed = 0;
        while (completed < limit && _chunkRequestCallbacks.TryDequeue(out ChunkRequestCallback ready))
        {
            ready.Callback(ready.Chunk);
            completed++;
        }

        while (completed < limit && _completedChunkRequests.TryDequeue(out CompletedChunkRequest completedRequest))
        {
            PendingChunkRequest? request;
            lock (_chunkRequestLock)
            {
                if (!_pendingChunkRequests.Remove(completedRequest.Hash, out request))
                {
                    continue;
                }
            }

            if (completedRequest.Chunk is null)
            {
                continue;
            }

            _chunks[completedRequest.Hash] = completedRequest.Chunk;

            foreach (Action<ChunkColumn> callback in request.Callbacks)
            {
                callback(completedRequest.Chunk);
            }

            completed++;
        }
    }

    private async Task ChunkRequestWorker()
    {
        CancellationToken token = _chunkRequestCancel.Token;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await _chunkRequestSignal.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!_chunkRequests.TryDequeue(out long hash))
            {
                continue;
            }

            int x = (int)(hash >> 32);
            int z = (int)hash;

            try
            {
                ChunkColumn? loaded = _provider.LoadChunk(Type, x, z);
                if (loaded is null)
                {
                    loaded = _generator.Generate(Type, x, z);
                    _generator.Populate(loaded);
                    loaded.Dirty = true;
                }

                _completedChunkRequests.Enqueue(new CompletedChunkRequest(hash, loaded));
            }
            catch (Exception exception)
            {
                Logger.Err($"Failed to request chunk {x}, {z}: {exception.Message}");
                _completedChunkRequests.Enqueue(new CompletedChunkRequest(hash, null));
            }
        }
    }

    private sealed class PendingChunkRequest
    {
        public readonly List<Action<ChunkColumn>> Callbacks;

        public PendingChunkRequest(Action<ChunkColumn> callback)
        {
            Callbacks = [callback];
        }
    }

    private readonly record struct CompletedChunkRequest(long Hash, ChunkColumn? Chunk);
    private readonly record struct ChunkRequestCallback(ChunkColumn Chunk, Action<ChunkColumn> Callback);
}







