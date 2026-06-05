using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public sealed class InMemoryProvider : WorldProvider
{
    private readonly Dictionary<long, ChunkColumn> _chunks = [];
    public override string Identifier => "memory";
    public override bool HasChunk(DimensionType dimensionType, int x, int z) => _chunks.ContainsKey(HashChunk(x, z));

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z)
    {
        _chunks.TryGetValue(HashChunk(x, z), out ChunkColumn? chunk);
        return chunk;
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        _chunks[HashChunk(chunk.X, chunk.Z)] = chunk;
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        _chunks.Remove(HashChunk(x, z));
    }

    public override void Dispose()
    {
    }
}







