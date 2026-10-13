using BasaltBE.Basalt.Blocks;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds.Dimensions.Chunks;

public sealed class BlockStorage {
    public const int Size = 16;
    public const int CellCount = Size * Size * Size;
    public static readonly BlockPermutation Air = BlockPalette.FindDefaultPermutation("minecraft:air")
        ?? throw new InvalidOperationException("The air block permutation is missing.");

    ushort[]? indices;
    BlockPermutation implicitPermutation = Air;
    List<BlockPermutation>? palette;
    Dictionary<BlockPermutation, ushort>? paletteIndices;
    int nonAirCount;

    public bool Empty => nonAirCount == 0;

    public BlockPermutation GetPermutation(int x, int y, int z) => GetPermutationAt(GetIndex(x, y, z));

    internal BlockPermutation GetPermutationAt(int index) => indices is null ? implicitPermutation : palette![indices[index]];

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation) {
        ArgumentNullException.ThrowIfNull(permutation);
        SetPermutationAt(GetIndex(x, y, z), permutation);
    }

    internal void SetPermutationAt(int index, BlockPermutation permutation) {
        if (indices is null) {
            if (permutation == implicitPermutation) {
                return;
            }

            indices = new ushort[CellCount];
            palette = [implicitPermutation];
            paletteIndices = new Dictionary<BlockPermutation, ushort> { [implicitPermutation] = 0 };
        }

        ushort oldIndex = indices[index];
        if (palette![oldIndex] == permutation) {
            return;
        }

        bool wasAir = palette[oldIndex] == Air;
        bool isAir = permutation == Air;
        if (wasAir && !isAir) {
            nonAirCount++;
        } else if (!wasAir && isAir) {
            nonAirCount--;
        }

        if (!paletteIndices!.TryGetValue(permutation, out ushort paletteIndex)) {
            if (palette!.Count > CellCount) {
                throw new InvalidOperationException("A subchunk cannot contain more unique permutations than cells.");
            }

            paletteIndex = checked((ushort)palette.Count);
            palette.Add(permutation);
            paletteIndices.Add(permutation, paletteIndex);
        }

        indices[index] = paletteIndex;
    }

    public void Serialize(ref BinaryWriter writer) {
        int paletteCount = palette?.Count ?? 1;
        int bitsPerBlock = PaletteEncoding.GetBitsPerEntry(paletteCount);
        writer.WriteUInt8((byte)((bitsPerBlock << 1) | 1));

        if (bitsPerBlock > 0) {
            int valuesPerWord = 32 / bitsPerBlock;
            int wordCount = (CellCount + valuesPerWord - 1) / valuesPerWord;
            uint mask = (1u << bitsPerBlock) - 1;
            for (int wordIndex = 0; wordIndex < wordCount; wordIndex++) {
                uint word = 0;
                for (int valueIndex = 0; valueIndex < valuesPerWord; valueIndex++) {
                    int cellIndex = wordIndex * valuesPerWord + valueIndex;
                    if (cellIndex >= CellCount) {
                        break;
                    }

                    uint paletteIndex = indices is null ? 0u : indices[cellIndex];
                    word |= (paletteIndex & mask) << (valueIndex * bitsPerBlock);
                }

                writer.WriteUInt32(word, true);
            }
        }

        writer.WriteZigZag(paletteCount);
        if (palette is null) {
            writer.WriteZigZag(implicitPermutation.NetworkId);
        } else {
            foreach (BlockPermutation permutation in palette) {
                writer.WriteZigZag(permutation.NetworkId);
            }
        }
    }

    public static BlockStorage Deserialize(ref BinaryReader reader) {
        byte format = reader.ReadUInt8();
        if ((format & 1) == 0) {
            throw new InvalidDataException("The block palette is not in network format.");
        }

        int bitsPerBlock = format >> 1;
        if (bitsPerBlock is not (0 or 1 or 2 or 3 or 4 or 5 or 6 or 8 or 16)) {
            throw new InvalidDataException($"Invalid bits per block: {bitsPerBlock}.");
        }

        ushort[]? indices = null;
        int valuesPerWord = 0;
        int wordCount = 0;
        if (bitsPerBlock > 0) {
            valuesPerWord = 32 / bitsPerBlock;
            wordCount = (CellCount + valuesPerWord - 1) / valuesPerWord;
            indices = new ushort[CellCount];
            uint mask = (1u << bitsPerBlock) - 1;
            int cellIndex = 0;
            for (int i = 0; i < wordCount; i++) {
                uint word = reader.ReadUInt32(true);
                int count = Math.Min(valuesPerWord, CellCount - cellIndex);
                for (int valueIndex = 0; valueIndex < count; valueIndex++, cellIndex++) {
                    indices[cellIndex] = (ushort)((word >> (valueIndex * bitsPerBlock)) & mask);
                }
            }
        }

        int paletteCount = reader.ReadZigZag();
        if (paletteCount is < 1 or > CellCount + 1 || (bitsPerBlock == 0 && paletteCount != 1) ||
            (bitsPerBlock > 0 && paletteCount > 1 << bitsPerBlock)) {
            throw new InvalidDataException("Invalid block palette size.");
        }

        BlockStorage storage = new();
        for (int i = 0; i < paletteCount; i++) {
            int networkId = reader.ReadZigZag();
            BlockPermutation permutation = BlockPalette.FindByNetworkId(networkId)
                ?? throw new InvalidDataException($"Unknown block network ID {networkId}.");

            if (bitsPerBlock == 0) {
                storage.implicitPermutation = permutation;
                continue;
            }

            storage.palette ??= new List<BlockPermutation>(paletteCount);
            storage.paletteIndices ??= new Dictionary<BlockPermutation, ushort>(paletteCount);
            storage.palette.Add(permutation);
            storage.paletteIndices.Add(permutation, checked((ushort)i));
        }

        if (bitsPerBlock == 0) {
            storage.nonAirCount = storage.implicitPermutation == Air ? 0 : CellCount;
        } else {
            storage.indices = indices;
            for (int cellIndex = 0; cellIndex < CellCount; cellIndex++) {
                ushort paletteIndex = indices![cellIndex];
                if (paletteIndex >= paletteCount) {
                    throw new InvalidDataException("Block palette index is out of range.");
                }

                if (storage.palette![paletteIndex] != Air) {
                    storage.nonAirCount++;
                }
            }
        }

        return storage;
    }

    internal static int GetIndex(int x, int y, int z) {
        if ((uint)(x | y | z) >= Size) {
            throw new ArgumentOutOfRangeException(nameof(x), "Subchunk coordinates must be from 0 to 15.");
        }

        return (x << 8) | (z << 4) | y;
    }
}
