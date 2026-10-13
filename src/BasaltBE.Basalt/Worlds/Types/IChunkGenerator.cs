using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Worlds.Types;

public interface IChunkGenerator {
    Chunk Generate(int chunkX, int chunkZ);
}
