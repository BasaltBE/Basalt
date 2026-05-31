using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Server.World.Dimension.Chunk.Chunk;

namespace Basalt.Server.World.Dimension.Generation;

public sealed class VoidGenerator : Generator
{
    public override string Identifier => "void";

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z)
    {
        return new ChunkColumn(x, z, dimensionType);
    }
}







