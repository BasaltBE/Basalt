using Basalt.Block;
using Basalt.Protocol.Enums;
using Basalt.World.Dimension.Generation;
using Basalt.World.Dimension.Provider;
// Damn name spacing
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension;

public sealed class Dimension : IDisposable
{
    private readonly Dictionary<long, ChunkColumn> _chunks;
    private readonly WorldProvider _provider;
    private readonly Generator _generator;

    public string Identifier { get; }
    public DimensionType Type { get; }

    public Dimension(string identifier, DimensionType type, WorldProvider provider, Generator? generator = null)
    {
        Identifier = identifier;
        Type = type;
        _chunks = [];
        _provider = provider;
        _generator = generator ?? new VoidGenerator();
    }

    public int ChunkCount => _chunks.Count;

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
        return _chunks.Remove(HashChunk(x, z));
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

        return _chunks.Remove(hash);
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

    private static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
}
