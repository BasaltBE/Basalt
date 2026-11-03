using BasaltBE.Basalt.Blocks;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds.Dimensions.Chunks;

public enum ChunkSerializationMode {
    Network,
    LevelDb
}

public sealed class SubChunk {
    List<BlockStorage>? layers;

    internal Chunk? Chunk;
    public BiomeStorage Biomes = new();
    public sbyte Index;

    public bool Empty {
        get {
            if (layers is null) {
                return true;
            }

            foreach (BlockStorage layer in layers) {
                if (!layer.Empty) {
                    return false;
                }
            }

            return true;
        }
    }

    public int LayerCount => layers?.Count ?? 0;

    internal bool RandomTicking => layers is { Count: > 0 } && layers[0].RandomTickCount > 0;

    internal bool HasRedstone {
        get {
            if (layers is null)
                return false;

            foreach (BlockStorage layer in layers) {
                if (layer.HasRedstone)
                    return true;
            }

            return false;
        }
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        return GetPermutationAt(BlockStorage.GetIndex(x, y, z), layer);
    }

    internal BlockPermutation GetPermutationAt(int index, int layer = 0) =>
        layers is not null && (uint)layer < (uint)layers.Count ? layers[layer].GetPermutationAt(index) : BlockStorage.Air;

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0) {
        ArgumentNullException.ThrowIfNull(permutation);
        SetPermutationAt(BlockStorage.GetIndex(x, y, z), permutation, layer);
    }

    internal void SetPermutationAt(int index, BlockPermutation permutation, int layer = 0) {
        ArgumentNullException.ThrowIfNull(permutation);
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int layerCount = layers?.Count ?? 0;
        if (permutation == BlockStorage.Air && (uint)layer >= (uint)layerCount) {
            return;
        }

        if (GetPermutationAt(index, layer) == permutation)
            return;

        layers ??= [];
        while (layers.Count <= layer) {
            layers.Add(new BlockStorage());
        }

        layers[layer].SetPermutationAt(index, permutation);
        Chunk?.MarkDirty();
    }

    public int GetBiome(int x, int y, int z) => Biomes.Initialized
        ? Biomes.GetBiome(x, y, z)
        : Chunk?.Dimension?.CustomDefinition?.DefaultBiomeId ?? Biomes.GetBiome(x, y, z);

    public void SetBiome(int x, int y, int z, int biome) {
        if (GetBiome(x, y, z) == biome)
            return;
        if (!Biomes.Initialized && Chunk?.Dimension?.CustomDefinition is { } definition)
            Biomes = new BiomeStorage(definition.DefaultBiomeId);
        Biomes.SetBiome(x, y, z, biome);
        Chunk?.MarkDirty();
    }

    public void Serialize(ref BinaryWriter writer, sbyte index, ChunkSerializationMode mode = ChunkSerializationMode.Network) {
        int layerCount = layers?.Count ?? 0;
        if (layerCount > 2) {
            throw new InvalidOperationException("A subchunk supports two block layers.");
        }

        writer.WriteUInt8(9);
        writer.WriteUInt8((byte)layerCount);
        writer.WriteInt8(index);
        if (layers is null) {
            return;
        }

        foreach (BlockStorage layer in layers) {
            layer.Serialize(ref writer, mode);
        }
    }

    public static SubChunk Deserialize(
        ref BinaryReader reader,
        ChunkSerializationMode mode = ChunkSerializationMode.Network
    ) => Deserialize(ref reader, mode, true)!;

    internal static SubChunk? Deserialize(ref BinaryReader reader, ChunkSerializationMode mode, bool retainEmpty) {
        byte version = reader.ReadUInt8();
        if (version != 9) {
            throw new InvalidDataException($"Unsupported subchunk version {version}.");
        }

        byte layerCount = reader.ReadUInt8();
        if (layerCount > 2) {
            throw new InvalidDataException("A subchunk cannot contain more than two block layers.");
        }

        sbyte index = reader.ReadInt8();
        if (layerCount == 0 && !retainEmpty) {
            return null;
        }

        SubChunk subChunk = new() { Index = index };
        for (int i = 0; i < layerCount; i++) {
            (subChunk.layers ??= []).Add(BlockStorage.Deserialize(ref reader, mode));
        }

        return subChunk;
    }

}
