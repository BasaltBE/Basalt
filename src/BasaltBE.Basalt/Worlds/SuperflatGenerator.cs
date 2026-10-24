using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class SuperflatGenerator : IChunkGenerator {
    static readonly BlockPermutation Bedrock = Find("minecraft:bedrock");
    static readonly BlockPermutation Dirt = Find("minecraft:dirt");
    static readonly BlockPermutation Grass = Find("minecraft:grass_block");

    public Chunk Generate(Dimension dimension, int chunkX, int chunkZ) {
        Chunk chunk = new(chunkX, chunkZ);
        SetLayer(chunk, dimension.MinimumY, Bedrock);
        SetLayer(chunk, dimension.MinimumY + 1, Dirt);
        SetLayer(chunk, dimension.MinimumY + 2, Dirt);
        SetLayer(chunk, dimension.MinimumY + 3, Grass);
        return chunk;
    }

    static void SetLayer(Chunk chunk, int y, BlockPermutation permutation) {
        for (int x = 0; x < 16; x++) {
            for (int z = 0; z < 16; z++)
                chunk.SetPermutation(x, y, z, permutation);
        }
    }

    static BlockPermutation Find(string identifier) =>
        BlockPalette.FindDefaultPermutation(identifier)
        ?? throw new InvalidOperationException($"Block permutation '{identifier}' is missing.");
}
