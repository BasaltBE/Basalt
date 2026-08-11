using Basalt.Core.Blocks;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

using BedrockProtocol.Nbt;

namespace Basalt.Core.Worlds.Dimensions.Chunk;

public sealed class BlockStorage {
    public const int MaxX = 16;
    public const int MaxY = 16;
    public const int MaxZ = 16;
    public const int MaxSize = MaxX * MaxY * MaxZ;

    public static readonly int Air = BlockPermutation.Resolve("minecraft:air").NetworkId;

    private readonly Dictionary<int, int> _paletteIndices;

    public List<int> Palette { get; }
    public int[] Blocks { get; }

    public BlockStorage(List<int>? palette = null, int[]? blocks = null) {
        Palette = palette ?? [Air];
        Blocks = blocks ?? new int[MaxSize];

        _paletteIndices = new Dictionary<int, int>(Palette.Count);
        for (int i = 0; i < Palette.Count; i++) {
            _paletteIndices[Palette[i]] = i;
        }
    }

    public bool IsEmpty() {
        return Palette.Count == 1 && Palette[0] == Air;
    }

    public int GetState(int bx, int by, int bz) {
        int paletteIndex = Blocks[GetIndex(bx, by, bz)];
        return (uint)paletteIndex < (uint)Palette.Count ? Palette[paletteIndex] : Air;
    }

    public void SetState(int bx, int by, int bz, int state) {
        if (!_paletteIndices.TryGetValue(state, out int paletteIndex)) {
            paletteIndex = Palette.Count;
            Palette.Add(state);
            _paletteIndices[state] = paletteIndex;
        }

        Blocks[GetIndex(bx, by, bz)] = paletteIndex;
    }

    public static void Serialize(BlockStorage storage, BinaryWriter writer, bool nbt = false) {
        int bitsPerBlock = ResolveBitsPerValue(storage.Palette.Count, false);

        writer.WriteUInt8((byte)((bitsPerBlock << 1) | 1));

        int blocksPerWord = 32 / bitsPerBlock;
        int wordCount = (MaxSize + blocksPerWord - 1) / blocksPerWord;
        for (int w = 0; w < wordCount; w++) {
            int word = 0;
            for (int block = 0; block < blocksPerWord; block++) {
                int index = w * blocksPerWord + block;
                if (index >= MaxSize) {
                    break;
                }

                int state = storage.Blocks[index];
                word |= state << (block * bitsPerBlock);
            }

            writer.WriteInt32(word, littleEndian: true);
        }

        if (nbt) {
            writer.WriteInt32(storage.Palette.Count, littleEndian: true);
        }
        else {
            writer.WriteZigZag(storage.Palette.Count);
        }

        for (int i = 0; i < storage.Palette.Count; i++) {
            int state = storage.Palette[i];
            if (nbt) {
                BlockPermutation permutation = BlockPermutation.Resolve(state);
                CompoundTag tag = BlockPermutation.ToCompound(permutation);
                NBT.WriteTag(writer, tag, new TagOptions(Name: true, Type: true, VarInt: false));
            }
            else {
                writer.WriteZigZag(state);
            }
        }
    }

    public static BlockStorage Deserialize(ref BinaryReader reader, bool nbt = false) {
        byte paletteAndFlag = reader.ReadUInt8();
        int bitsPerBlock = paletteAndFlag >> 1;

        if (bitsPerBlock > 16) {
            throw new InvalidOperationException($"Invalid bits per block: {bitsPerBlock}.");
        }

        if (bitsPerBlock == 0) {
            int zeroPaletteSize = nbt ? reader.ReadInt32(littleEndian: true) : reader.ReadZigZag();
            if (zeroPaletteSize <= 0) {
                throw new InvalidOperationException("Invalid block palette size.");
            }

            List<int> zeroPalette = new(zeroPaletteSize);
            for (int i = 0; i < zeroPaletteSize; i++) {
                if (nbt) {
                    TagType tagType = (TagType)reader.ReadInt8();
                    if (tagType != TagType.Compound) {
                        throw new InvalidOperationException($"Expected Compound tag, got {tagType}.");
                    }

                    CompoundTag tag = CompoundTag.Read(reader, new TagOptions(Name: true, Type: false, VarInt: false));
                    zeroPalette.Add(BlockPermutation.FromCompound(tag).NetworkId);
                }
                else {
                    zeroPalette.Add(reader.ReadZigZag());
                }
            }

            return new BlockStorage(zeroPalette, new int[MaxSize]);
        }

        int blocksPerWord = 32 / bitsPerBlock;
        int wordCount = (MaxSize + blocksPerWord - 1) / blocksPerWord;

        int[] words = new int[wordCount];
        for (int i = 0; i < wordCount; i++) {
            words[i] = reader.ReadInt32(littleEndian: true);
        }

        int paletteSize = nbt ? reader.ReadInt32(littleEndian: true) : reader.ReadZigZag();
        if (paletteSize <= 0) {
            throw new InvalidOperationException("Invalid block palette size.");
        }

        List<int> palette = new(paletteSize);
        for (int i = 0; i < paletteSize; i++) {
            if (nbt) {
                TagType tagType = (TagType)reader.ReadInt8();
                if (tagType != TagType.Compound) {
                    throw new InvalidOperationException($"Expected Compound tag, got {tagType}.");
                }

                CompoundTag tag = CompoundTag.Read(reader, new TagOptions(Name: true, Type: false, VarInt: false));
                palette.Add(BlockPermutation.FromCompound(tag).NetworkId);
            }
            else {
                palette.Add(reader.ReadZigZag());
            }
        }

        int[] blocks = new int[MaxSize];
        int position = 0;
        int mask = (1 << bitsPerBlock) - 1;

        for (int w = 0; w < words.Length && position < MaxSize; w++) {
            int word = words[w];
            for (int block = 0; block < blocksPerWord && position < MaxSize; block++, position++) {
                blocks[position] = (word >> (block * bitsPerBlock)) & mask;
            }
        }

        return new BlockStorage(palette, blocks);
    }

    private static int GetIndex(int bx, int by, int bz) {
        return ((bx & 0xF) << 8) | ((bz & 0xF) << 4) | (by & 0xF);
    }

    private static int ResolveBitsPerValue(int paletteLength, bool allowZero) {
        int bits = (int)Math.Ceiling(Math.Log2(Math.Max(1, paletteLength)));

        switch (bits) {
            case 0:
                return allowZero ? 0 : 1;
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
                return bits;
            case 7:
            case 8:
                return 8;
            default:
                return 16;
        }
    }
}







