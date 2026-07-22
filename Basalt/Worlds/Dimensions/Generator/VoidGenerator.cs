using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Generation;

public sealed class VoidGenerator : Generator {
    public override string Identifier => "void";

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z) {
        return new ChunkColumn(x, z, dimensionType);
    }
}







