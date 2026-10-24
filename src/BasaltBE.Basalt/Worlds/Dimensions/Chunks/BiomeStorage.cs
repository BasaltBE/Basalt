using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds.Dimensions.Chunks;

public sealed class BiomeStorage {
    public const int CellCount = BlockStorage.CellCount;

    ushort[]? indices;
    List<int>? palette;
    Dictionary<int, ushort>? paletteIndices;
    int singletonBiome;
    int nonDefaultCount;

    public BiomeStorage(int defaultBiome = 0) {
        singletonBiome = defaultBiome;
        nonDefaultCount = defaultBiome == 0 ? 0 : CellCount;
    }

    public bool Empty => nonDefaultCount == 0;
    internal bool Initialized;

    public int GetBiome(int x, int y, int z) {
        int index = BlockStorage.GetIndex(x, y, z);
        return indices is null ? singletonBiome : palette![indices[index]];
    }

    public void SetBiome(int x, int y, int z, int biome) {
        int index = BlockStorage.GetIndex(x, y, z);
        if (indices is null) {
            if (biome == singletonBiome) {
                return;
            }

            indices = new ushort[CellCount];
            palette = [singletonBiome];
            paletteIndices = new Dictionary<int, ushort> { [singletonBiome] = 0 };
        }

        ushort oldIndex = indices[index];
        int oldBiome = palette![oldIndex];
        if (oldBiome == biome) {
            return;
        }

        Initialized = true;

        if (oldBiome == 0 && biome != 0) {
            nonDefaultCount++;
        }
        else if (oldBiome != 0 && biome == 0) {
            nonDefaultCount--;
        }

        if (!paletteIndices!.TryGetValue(biome, out ushort paletteIndex)) {
            if (palette!.Count > CellCount) {
                throw new InvalidOperationException("A subchunk cannot contain more unique biomes than cells.");
            }

            paletteIndex = checked((ushort)palette.Count);
            palette.Add(biome);
            paletteIndices.Add(biome, paletteIndex);
        }

        indices[index] = paletteIndex;
    }

    public void Serialize(ref BinaryWriter writer, ChunkSerializationMode mode = ChunkSerializationMode.Network) {
        int paletteCount = palette?.Count ?? 1;
        int bitsPerBiome = PaletteEncoding.GetBitsPerEntry(paletteCount);
        writer.WriteUInt8((byte)((bitsPerBiome << 1) | 1));

        if (bitsPerBiome == 0) {
            if (mode == ChunkSerializationMode.Network)
                writer.WriteZigZag(singletonBiome);
            else
                writer.WriteInt32(singletonBiome, true);
            return;
        }

        int valuesPerWord = 32 / bitsPerBiome;
        int wordCount = (CellCount + valuesPerWord - 1) / valuesPerWord;
        uint mask = (1u << bitsPerBiome) - 1;
        for (int wordIndex = 0; wordIndex < wordCount; wordIndex++) {
            uint word = 0;
            for (int valueIndex = 0; valueIndex < valuesPerWord; valueIndex++) {
                int cellIndex = wordIndex * valuesPerWord + valueIndex;
                if (cellIndex >= CellCount) {
                    break;
                }

                word |= ((uint)indices![cellIndex] & mask) << (valueIndex * bitsPerBiome);
            }

            writer.WriteUInt32(word, true);
        }

        if (mode == ChunkSerializationMode.Network) {
            writer.WriteZigZag(paletteCount);
            foreach (int biome in palette!)
                writer.WriteZigZag(biome);
        }
        else {
            writer.WriteInt32(paletteCount, true);
            foreach (int biome in palette!)
                writer.WriteInt32(biome, true);
        }
    }

    internal static void SerializeDefault(
        ref BinaryWriter writer,
        int biome = 0,
        ChunkSerializationMode mode = ChunkSerializationMode.Network
    ) {
        writer.WriteUInt8(1);
        if (mode == ChunkSerializationMode.Network)
            writer.WriteZigZag(biome);
        else
            writer.WriteInt32(biome, true);
    }

    public static BiomeStorage Deserialize(ref BinaryReader reader) =>
        Deserialize(ref reader, ChunkSerializationMode.Network, true)!;

    internal static BiomeStorage? Deserialize(ref BinaryReader reader, ChunkSerializationMode mode, bool retainEmpty) {
        byte format = reader.ReadUInt8();
        if ((format & 1) == 0) {
            throw new InvalidDataException("The biome palette is not in network format.");
        }

        int bitsPerBiome = format >> 1;
        if (bitsPerBiome is not (0 or 1 or 2 or 3 or 4 or 5 or 6 or 8 or 16)) {
            throw new InvalidDataException($"Invalid bits per biome: {bitsPerBiome}.");
        }

        if (bitsPerBiome == 0) {
            int biome = mode == ChunkSerializationMode.Network ? reader.ReadZigZag() : reader.ReadInt32(true);
            if (biome == 0 && !retainEmpty) {
                return null;
            }
            return new BiomeStorage {
                singletonBiome = biome,
                nonDefaultCount = biome == 0 ? 0 : CellCount,
                Initialized = true
            };
        }

        int valuesPerWord = 32 / bitsPerBiome;
        int wordCount = (CellCount + valuesPerWord - 1) / valuesPerWord;
        ushort[] indices = new ushort[CellCount];
        uint mask = (1u << bitsPerBiome) - 1;
        int cellIndex = 0;
        for (int i = 0; i < wordCount; i++) {
            uint word = reader.ReadUInt32(true);
            int count = Math.Min(valuesPerWord, CellCount - cellIndex);
            for (int valueIndex = 0; valueIndex < count; valueIndex++, cellIndex++) {
                indices[cellIndex] = (ushort)((word >> (valueIndex * bitsPerBiome)) & mask);
            }
        }

        int paletteCount = mode == ChunkSerializationMode.Network ? reader.ReadZigZag() : reader.ReadInt32(true);
        if (paletteCount is < 2 or > CellCount + 1 || paletteCount > 1 << bitsPerBiome) {
            throw new InvalidDataException("Invalid biome palette size.");
        }

        BiomeStorage storage = new();
        storage.palette = new List<int>(paletteCount);
        storage.paletteIndices = new Dictionary<int, ushort>(paletteCount);
        for (int i = 0; i < paletteCount; i++) {
            int biome = mode == ChunkSerializationMode.Network ? reader.ReadZigZag() : reader.ReadInt32(true);
            storage.palette.Add(biome);
            storage.paletteIndices.Add(biome, checked((ushort)i));
        }

        storage.indices = indices;
        storage.Initialized = true;
        for (int i = 0; i < CellCount; i++) {
            ushort paletteIndex = indices[i];
            if (paletteIndex >= paletteCount) {
                throw new InvalidDataException("Biome palette index is out of range.");
            }

            if (storage.palette[paletteIndex] != 0) {
                storage.nonDefaultCount++;
            }
        }

        return storage;
    }

}
