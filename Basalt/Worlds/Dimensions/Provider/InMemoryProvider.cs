using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

using System.Collections.Concurrent;

public sealed class InMemoryProvider : WorldProvider {
    private readonly ConcurrentDictionary<(DimensionId, long), ChunkColumn> _chunks = [];
    public override string Identifier => "memory";
    public override bool HasChunk(DimensionId dimensionType, int x, int z) => _chunks.ContainsKey((dimensionType, HashChunk(x, z)));

    public override ChunkColumn? LoadChunk(DimensionId dimensionType, int x, int z) {
        _chunks.TryGetValue((dimensionType, HashChunk(x, z)), out ChunkColumn? chunk);
        return chunk;
    }

    public override void SaveChunk(ChunkColumn chunk) {
        _chunks[(chunk.Type, HashChunk(chunk.X, chunk.Z))] = chunk;
    }

    public override void DeleteChunk(DimensionId dimensionType, int x, int z) {
        _chunks.TryRemove((dimensionType, HashChunk(x, z)), out _);
    }

    public override void Dispose() {
    }
}







