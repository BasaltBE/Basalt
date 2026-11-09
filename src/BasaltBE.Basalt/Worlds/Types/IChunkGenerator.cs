using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Worlds.Types;

public interface IChunkGenerator {
    Chunk Generate(Dimension dimension, int chunkX, int chunkZ);
}
