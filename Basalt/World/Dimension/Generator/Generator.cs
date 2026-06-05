using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.World.Dimension.Chunk.Chunk;

namespace Basalt.Core.World.Dimension.Generation;

public abstract class Generator
{
    public abstract string Identifier { get; }
    public abstract ChunkColumn Generate(DimensionType dimensionType, int x, int z);
    public virtual void Populate(ChunkColumn chunk)
    {
    }
}







