using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.World.Dimension.Chunk.Chunk;

namespace Basalt.Core.World.Dimension.Generation;

public sealed class VoidGenerator : Generator
{
    public override string Identifier => "void";

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z)
    {
        return new ChunkColumn(x, z, dimensionType);
    }
}







