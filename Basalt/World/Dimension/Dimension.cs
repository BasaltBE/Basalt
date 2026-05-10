using Basalt.Block;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using Basalt.World.Dimension.Generation;
using Basalt.World.Dimension.Provider;
// Damn name spacing
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension;

public sealed class Dimension : IDisposable
{
    private readonly Dictionary<long, ChunkColumn> _chunks;
    private readonly Dictionary<long, int> _chunkViewers;
    private readonly HashSet<global::Basalt.Entity.Entity> _entities;
    private readonly WorldProvider _provider;
    private readonly Generator _generator;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public global::Basalt.World.World? World { get; internal set; }
    public global::Basalt.World.DimensionGameRules Gamerules { get; } = new();
    internal Action<DataPacket>? PacketBroadcaster { get; set; }
    internal Action<DataPacket, global::Basalt.Entity.Entity>? PacketBroadcasterExcept { get; set; }

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
    public IReadOnlyCollection<global::Basalt.Entity.Entity> Entities => _entities;

    public bool HasChunk(int x, int z)
    {
        long hash = HashChunk(x, z);
        return _chunks.ContainsKey(hash) || _provider.HasChunk(x, z);
    }

    public ChunkColumn? GetChunk(int x, int z)
    {
        long hash = HashChunk(x, z);
        if (_chunks.TryGetValue(hash, out ChunkColumn? value))
        {
            return value;
        }

        ChunkColumn? loaded = _provider.LoadChunk(Type, x, z);
        if (loaded is not null)
        {
            _chunks[hash] = loaded;
            return loaded;
        }

        return null;
    }

    public ChunkColumn GetOrCreateChunk(int x, int z)
    {
        long hash = HashChunk(x, z);
        if (_chunks.TryGetValue(hash, out ChunkColumn? chunk))
        {
            return chunk;
        }

        ChunkColumn? loaded = _provider.LoadChunk(Type, x, z);
        if (loaded is not null)
        {
            _chunks[hash] = loaded;
            return loaded;
        }

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
        _provider.DeleteChunk(x, z);
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
            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }

        chunk.ReleaseMemory();
        return _chunks.Remove(hash);
    }

    public void AddChunkViewer(int x, int z)
    {
        long hash = HashChunk(x, z);
        if (_chunkViewers.TryGetValue(hash, out int count))
        {
            _chunkViewers[hash] = count + 1;
            return;
        }

        _chunkViewers[hash] = 1;
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
        List<long> hashes = [.. _chunks.Keys];
        for (int i = 0; i < hashes.Count && unloaded < limit; i++)
        {
            long hash = hashes[i];
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
        return chunk.GetPermutation(x & 0xF, y, z & 0xF, layer);
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetPermutation(x & 0xF, y, z & 0xF, permutation, layer, dirty);
    }

    public int GetBiome(int x, int y, int z)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetBiome(x & 0xF, y, z & 0xF);
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBiome(x & 0xF, y, z & 0xF, biomeId, dirty);
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

        foreach (global::Basalt.Entity.Entity entity in _entities.ToArray())
        {
            if (!entity.IsAlive || entity.Dimension != this)
            {
                continue;
            }

            entity.TickTraits(currentTick, deltaTick);
        }
    }

    public void Broadcast(DataPacket packet)
    {
        PacketBroadcaster?.Invoke(packet);
    }

    public void BroadcastExcept(DataPacket packet, global::Basalt.Entity.Entity exclude)
    {
        PacketBroadcasterExcept?.Invoke(packet, exclude);
    }

    internal void AddEntity(global::Basalt.Entity.Entity entity)
    {
        _entities.Add(entity);
    }

    internal void RemoveEntity(global::Basalt.Entity.Entity entity)
    {
        _entities.Remove(entity);
    }

    private static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
}
