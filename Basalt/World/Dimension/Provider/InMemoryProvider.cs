using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension.Provider;

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
