using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class SuperflatGenerator : IChunkGenerator {
    static readonly BlockPermutation Bedrock = Find("minecraft:bedrock");
    static readonly BlockPermutation Dirt = Find("minecraft:dirt");
    static readonly BlockPermutation Grass = Find("minecraft:grass_block");

    public Chunk Generate(int chunkX, int chunkZ) {
        Chunk chunk = new(chunkX, chunkZ);
        SetLayer(chunk, -64, Bedrock);
        SetLayer(chunk, -63, Dirt);
        SetLayer(chunk, -62, Dirt);
        SetLayer(chunk, -61, Grass);
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
