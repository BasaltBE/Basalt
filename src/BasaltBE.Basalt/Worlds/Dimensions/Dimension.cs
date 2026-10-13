using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunk;
using WorldChunk = BasaltBE.Basalt.Worlds.Dimensions.Chunk.Chunk;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed class Dimension(string identifier) {
    public readonly string Identifier = identifier;

    readonly Dictionary<long, WorldChunk> chunks = [];

    public int ChunkCount => chunks.Count;

    public bool Empty {
        get {
            foreach (WorldChunk chunk in chunks.Values) {
                if (!chunk.Empty) {
                    return false;
                }
            }

            return true;
        }
    }

    public WorldChunk? GetChunk(int chunkX, int chunkZ) =>
        chunks.GetValueOrDefault(GetKey(chunkX, chunkZ));

    public WorldChunk GetOrCreateChunk(int chunkX, int chunkZ) {
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out WorldChunk? chunk)) {
            return chunk;
        }

        chunk = new WorldChunk(chunkX, chunkZ);
        chunks.Add(key, chunk);
        return chunk;
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        return chunks.TryGetValue(GetKey(chunkX, chunkZ), out WorldChunk? chunk)
            ? chunk.GetPermutation(x & 15, y, z & 15, layer)
            : BlockStorage.Air;
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0) {
        ArgumentNullException.ThrowIfNull(permutation);
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        if (!chunks.TryGetValue(GetKey(chunkX, chunkZ), out WorldChunk? chunk)) {
            if (permutation == BlockStorage.Air) {
                return;
            }

            chunk = GetOrCreateChunk(chunkX, chunkZ);
        }

        chunk.SetPermutation(x & 15, y, z & 15, permutation, layer);
    }

    static long GetKey(int x, int z) => ((long)x << 32) | (uint)z;
}
