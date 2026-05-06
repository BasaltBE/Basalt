using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension.Generation;

public sealed class VoidGenerator : Generator
{
    public override string Identifier => "void";

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z)
    {
        return new ChunkColumn(x, z, dimensionType);
    }
}
