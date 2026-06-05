using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Server.World.Dimension.Chunk.Chunk;

namespace Basalt.Server.World.Dimension.Generation;

public abstract class Generator
{
    public abstract string Identifier { get; }
    public abstract ChunkColumn Generate(DimensionType dimensionType, int x, int z);
    public virtual void Populate(ChunkColumn chunk)
    {
    }
}







