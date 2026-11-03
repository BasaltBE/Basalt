using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using NbtCodec = BasaltBE.Nbt.Nbt;

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
    List<byte[]?>? nativePaletteNbt;
    byte[]? nativeImplicitNbt;
    int nonAirCount;
    internal int RandomTickCount;

    public bool Empty => nonAirCount == 0;

    internal bool HasRedstone {
        get {
            if (palette is null)
                return IsRedstone(implicitPermutation);

            foreach (BlockPermutation permutation in palette) {
                if (IsRedstone(permutation))
                    return true;
            }

            return false;
        }
    }

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
            if (nativeImplicitNbt is not null)
                nativePaletteNbt = [nativeImplicitNbt];
        }

        ushort oldIndex = indices[index];
        BlockPermutation previous = palette![oldIndex];
        if (previous == permutation) {
            return;
        }

        if (BlockPalette.HasRandomTicks(previous.Identifier))
            RandomTickCount--;
        if (BlockPalette.HasRandomTicks(permutation.Identifier))
            RandomTickCount++;

        bool wasAir = previous == Air;
        bool isAir = permutation == Air;
        if (wasAir && !isAir) {
            nonAirCount++;
        }
        else if (!wasAir && isAir) {
            nonAirCount--;
        }

        if (!paletteIndices!.TryGetValue(permutation, out ushort paletteIndex)) {
            if (palette!.Count > CellCount) {
                throw new InvalidOperationException("A subchunk cannot contain more unique permutations than cells.");
            }

            paletteIndex = checked((ushort)palette.Count);
            palette.Add(permutation);
            paletteIndices.Add(permutation, paletteIndex);
            nativePaletteNbt?.Add(null);
        }

        indices[index] = paletteIndex;
    }

    public void Serialize(ref BinaryWriter writer, ChunkSerializationMode mode = ChunkSerializationMode.Network) {
        int paletteCount = palette?.Count ?? 1;
        int bitsPerBlock = Math.Max(1, PaletteEncoding.GetBitsPerEntry(paletteCount));
        writer.WriteUInt8((byte)((bitsPerBlock << 1) | (mode == ChunkSerializationMode.Network ? 1 : 0)));

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

        if (mode == ChunkSerializationMode.Network) {
            writer.WriteZigZag(paletteCount);
            if (palette is null) {
                writer.WriteZigZag(implicitPermutation.NetworkId);
            }
            else {
                foreach (BlockPermutation permutation in palette)
                    writer.WriteZigZag(permutation.NetworkId);
            }
            return;
        }

        writer.WriteInt32(paletteCount, true);
        if (palette is null) {
            if (nativeImplicitNbt is { } nativeNbt)
                writer.WriteBytes(nativeNbt);
            else
                WritePermutation(ref writer, implicitPermutation);
        }
        else {
            for (int i = 0; i < palette.Count; i++) {
                if (nativePaletteNbt is not null && nativePaletteNbt[i] is { } nativeNbt)
                    writer.WriteBytes(nativeNbt);
                else
                    WritePermutation(ref writer, palette[i]);
            }
        }
    }

    public static BlockStorage Deserialize(ref BinaryReader reader, ChunkSerializationMode mode = ChunkSerializationMode.Network) {
        byte format = reader.ReadUInt8();
        if (((format & 1) != 0) != (mode == ChunkSerializationMode.Network)) {
            throw new InvalidDataException("The block palette encoding does not match its chunk format.");
        }

        int bitsPerBlock = format >> 1;
        if (bitsPerBlock is not (0 or 1 or 2 or 3 or 4 or 5 or 6 or 8 or 16)) {
            throw new InvalidDataException($"Invalid bits per block: {bitsPerBlock}.");
        }

        ushort[]? indices = null;
        if (bitsPerBlock > 0) {
            int valuesPerWord = 32 / bitsPerBlock;
            int wordCount = (CellCount + valuesPerWord - 1) / valuesPerWord;
            indices = new ushort[CellCount];
            uint mask = (1u << bitsPerBlock) - 1;
            int cellIndex = 0;
            for (int wordIndex = 0; wordIndex < wordCount; wordIndex++) {
                uint word = reader.ReadUInt32(true);
                int count = Math.Min(valuesPerWord, CellCount - cellIndex);
                for (int valueIndex = 0; valueIndex < count; valueIndex++, cellIndex++)
                    indices[cellIndex] = (ushort)((word >> (valueIndex * bitsPerBlock)) & mask);
            }
        }

        int paletteCount = mode == ChunkSerializationMode.Network ? reader.ReadZigZag() : reader.ReadInt32(true);
        if (paletteCount is < 1 or > CellCount + 1 || (bitsPerBlock == 0 && paletteCount != 1) ||
            (bitsPerBlock > 0 && paletteCount > 1 << bitsPerBlock)) {
            throw new InvalidDataException("Invalid block palette size.");
        }

        BlockStorage storage = new();
        bool hasRandomTickingBlocks = false;
        for (int i = 0; i < paletteCount; i++) {
            BlockPermutation permutation;
            if (mode == ChunkSerializationMode.Network) {
                permutation = BlockPalette.FindByNetworkId(reader.ReadZigZag())
                    ?? throw new InvalidDataException("Unknown block network ID in palette.");
            }
            else {
                permutation = ReadPermutation(ref reader, out byte[] nativeNbt);
                storage.nativePaletteNbt ??= new List<byte[]?>(paletteCount);
                storage.nativePaletteNbt.Add(nativeNbt);
            }

            if (bitsPerBlock == 0) {
                storage.implicitPermutation = permutation;
                hasRandomTickingBlocks = BlockPalette.HasRandomTicks(permutation.Identifier);
                if (mode == ChunkSerializationMode.LevelDb)
                    storage.nativeImplicitNbt = storage.nativePaletteNbt![^1];
                continue;
            }

            hasRandomTickingBlocks |= BlockPalette.HasRandomTicks(permutation.Identifier);

            storage.palette ??= new List<BlockPermutation>(paletteCount);
            storage.paletteIndices ??= new Dictionary<BlockPermutation, ushort>(paletteCount);
            storage.palette.Add(permutation);
            storage.paletteIndices.TryAdd(permutation, checked((ushort)i));
        }

        if (bitsPerBlock == 0) {
            storage.nonAirCount = storage.implicitPermutation == Air ? 0 : CellCount;
            if (BlockPalette.HasRandomTicks(storage.implicitPermutation.Identifier))
                storage.RandomTickCount = CellCount;
        }
        else {
            storage.indices = indices;
            for (int cellIndex = 0; cellIndex < CellCount; cellIndex++) {
                ushort paletteIndex = indices![cellIndex];
                if (paletteIndex >= paletteCount) {
                    throw new InvalidDataException("Block palette index is out of range.");
                }

                if (storage.palette![paletteIndex] != Air) {
                    storage.nonAirCount++;
                }
                if (hasRandomTickingBlocks && BlockPalette.HasRandomTicks(storage.palette[paletteIndex].Identifier))
                    storage.RandomTickCount++;
            }
        }

        return storage;
    }

    static void WritePermutation(ref BinaryWriter writer, BlockPermutation permutation) =>
        NbtCodec.Write(CreatePermutationTag(permutation), ref writer, Options.LittleEndian);

    internal static CompoundTag CreatePermutationTag(BlockPermutation permutation) {
        CompoundTag state = new();
        CompoundTag blockStates = new();
        foreach ((string name, object value) in permutation.State.Values) {
            Tag tag = value switch {
                bool bit => new ByteTag { Value = bit ? (byte)1 : (byte)0 },
                byte number => new ByteTag { Value = number },
                short number => new ShortTag { Value = number },
                int number => new IntTag { Value = number },
                long number => new LongTag { Value = number },
                float number => new FloatTag { Value = number },
                double number => new DoubleTag { Value = number },
                string text => new StringTag { Value = text },
                _ => throw new InvalidDataException($"Unsupported block state value type {value.GetType()}.")
            };
            blockStates.Add(name, tag);
        }

        state.Add("name", new StringTag { Value = permutation.Identifier });
        state.Add("states", blockStates);
        state.Add("version", new IntTag { Value = 18168865 });
        return state;
    }

    static BlockPermutation ReadPermutation(ref BinaryReader reader, out byte[] nativeNbt) {
        int start = reader.Offset;
        if (NbtCodec.Read(ref reader, Options.LittleEndian) is not CompoundTag state)
            throw new InvalidDataException("A stored block palette entry has invalid NBT.");

        nativeNbt = reader.Buffer[start..reader.Offset].ToArray();
        BlockPermutation? permutation = ReadPermutation(state);
        if (permutation is null)
            throw new InvalidDataException("A stored block palette entry has an unknown block state.");

        return permutation;
    }

    internal static BlockPermutation? ReadPermutation(CompoundTag state) {
        if (state.Get<StringTag>("name") is not { } identifier ||
            state.Get<CompoundTag>("states") is not { } blockStates)
            return null;

        BlockType? type = BlockPalette.FindType(identifier.Value);
        if (type is null)
            return null;

        BlockState blockState = new();
        foreach ((string name, Tag tag) in blockStates.Tags) {
            if (Array.IndexOf(type.States, name) < 0)
                continue;

            object? stateValue = tag switch {
                ByteTag byteTag => BlockPalette.GetStateType(name) switch {
                    "bool" => byteTag.Value != 0,
                    "int" => (int)byteTag.Value,
                    _ => byteTag.Value
                },
                ShortTag shortTag => shortTag.Value,
                IntTag intTag => intTag.Value,
                LongTag longTag => longTag.Value,
                FloatTag floatTag => floatTag.Value,
                DoubleTag doubleTag => doubleTag.Value,
                StringTag stringTag => stringTag.Value,
                _ => null
            };
            if (stateValue is null)
                return null;
            blockState.Values.Add(name, stateValue);
        }

        return BlockPalette.FindStored(identifier.Value, blockState);
    }

    internal static int GetIndex(int x, int y, int z) {
        if ((uint)(x | y | z) >= Size) {
            throw new ArgumentOutOfRangeException(nameof(x), "Subchunk coordinates must be from 0 to 15.");
        }

        return (x << 8) | (z << 4) | y;
    }

    static bool IsRedstone(BlockPermutation permutation) =>
        permutation.Identifier is "minecraft:redstone_wire" or "minecraft:observer" or
            "minecraft:lever" or "minecraft:powered_repeater" or "minecraft:unpowered_repeater" or
            "minecraft:powered_comparator" or "minecraft:unpowered_comparator" or
            "minecraft:redstone_torch" or "minecraft:unlit_redstone_torch" or
            "minecraft:daylight_detector" or "minecraft:daylight_detector_inverted" ||
        permutation.State.Values.ContainsKey("button_pressed_bit") ||
        BlockPalette.FindType(permutation.Identifier)?.Components.RedstoneProducer is not null;
}
