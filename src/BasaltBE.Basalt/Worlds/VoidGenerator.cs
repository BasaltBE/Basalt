using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class VoidGenerator : IChunkGenerator {
    public Chunk Generate(int chunkX, int chunkZ) => new(chunkX, chunkZ);
}
