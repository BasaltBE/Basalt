namespace Basalt.Server.World.Dimension;

using Basalt.Server.Block;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Basalt.Server.World.Dimension.Generation;
using Basalt.Server.World.Dimension.Provider;
using ChunkColumn = Basalt.Server.World.Dimension.Chunk.Chunk;

using Entity = Basalt.Server.Entity.Entity;

public sealed class Dimension : IDisposable
{
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
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private bool _tickingEntities;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public global::Basalt.Server.World.World? World { get; internal set; }
    public global::Basalt.Server.World.DimensionGameRules Gamerules { get; } = new();

    public Dimension(string identifier, DimensionType type, WorldProvider provider, Generator? generator = null)
    {
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
            global::Basalt.Server.Block.Block? block = chunk.GetBlockActor(position);
            if (block is null)
            {
                block = new global::Basalt.Server.Block.Block(permutation);
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

    public global::Basalt.Server.Block.Block? GetBlock(int x, int y, int z)
    {
        ChunkColumn? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null)
        {
            return null;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };
        global::Basalt.Server.Block.Block? block = chunk.GetBlockActor(position);
        if (block is not null)
        {
            return block;
        }

        BlockPermutation perm = chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z));
        if (perm.Type.Traits.Count > 0)
        {
            block = new global::Basalt.Server.Block.Block(perm);
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

    public void SetBlock(int x, int y, int z, global::Basalt.Server.Block.Block block)
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
        SaveDirtyChunks();
    }

    public void Tick(ulong currentTick, uint deltaTick)
    {
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

        if (World?.Server is global::Basalt.Server.Server server)
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

            if (entity is global::Basalt.Server.Player.Player)
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
        if (World?.Server is not global::Basalt.Server.Server server)
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
        foreach (KeyValuePair<(int X, int Y, int Z), global::Basalt.Server.Block.Block> actorEntry in chunk.GetAllBlockActors())
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
}







