using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Generation;

public abstract class Generator {
    public abstract string Identifier { get; }
    public abstract ChunkColumn Generate(DimensionType dimensionType, int x, int z);
    public virtual void Populate(ChunkColumn chunk) {
    }
}







