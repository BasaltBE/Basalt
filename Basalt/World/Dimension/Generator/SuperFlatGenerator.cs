using Basalt.Server.Block;
using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.Server.World.Dimension.Chunk.Chunk;

namespace Basalt.Server.World.Dimension.Generation;

public sealed class SuperFlatGenerator : Generator
{
    private readonly (int Y, BlockPermutation Permutation)[] _layers;

    public override string Identifier => "superflat";

    public SuperFlatGenerator() : this(-64)
    {
    }

    public SuperFlatGenerator(int baseY)
    {
        _layers =
        [
            (baseY, BlockPermutation.Resolve(BlockIdentifier.Bedrock.ToIdentifier())),
            (baseY + 1, BlockPermutation.Resolve(BlockIdentifier.Dirt.ToIdentifier())),
            (baseY + 2, BlockPermutation.Resolve(BlockIdentifier.Dirt.ToIdentifier())),
            (baseY + 3, BlockPermutation.Resolve(BlockIdentifier.Dirt.ToIdentifier())),
            (baseY + 4, BlockPermutation.Resolve(BlockIdentifier.GrassBlock.ToIdentifier())),
        ];
    }

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z)
    {
        ChunkColumn chunk = new(x, z, dimensionType);

        for (int lx = 0; lx < 16; lx++)
        {
            for (int lz = 0; lz < 16; lz++)
            {
                for (int i = 0; i < _layers.Length; i++)
                {
                    (int y, BlockPermutation permutation) = _layers[i];
                    chunk.SetPermutation(lx, y, lz, permutation, layer: 0, dirty: false);
                }
            }
        }

        return chunk;
    }
}







