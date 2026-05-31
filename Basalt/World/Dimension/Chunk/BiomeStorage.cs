using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Server.World.Dimension.Chunk;

public sealed class BiomeStorage
{
    public const int MaxX = 16;
    public const int MaxY = 16;
    public const int MaxZ = 16;
    public const int MaxSize = MaxX * MaxY * MaxZ;

    private readonly Dictionary<int, int> _paletteIndices;

    public List<int> Palette { get; }
    public int[] Biomes { get; }

    public BiomeStorage(List<int>? palette = null, int[]? biomes = null)
    {
        Palette = palette ?? [0];
        Biomes = biomes ?? new int[MaxSize];

        _paletteIndices = new Dictionary<int, int>(Palette.Count);
        for (int i = 0; i < Palette.Count; i++)
        {
            _paletteIndices[Palette[i]] = i;
        }
    }

    public bool IsEmpty()
    {
        return Palette.Count == 1 && Palette[0] == 0;
    }

    public int GetBiome(int bx, int by, int bz)
    {
        int paletteIndex = Biomes[GetIndex(bx, by, bz)];
        return (uint)paletteIndex < (uint)Palette.Count ? Palette[paletteIndex] : 0;
    }

    public void SetBiome(int bx, int by, int bz, int biome)
    {
        if (!_paletteIndices.TryGetValue(biome, out int paletteIndex))
        {
            paletteIndex = Palette.Count;
            Palette.Add(biome);
            _paletteIndices[biome] = paletteIndex;
        }

        Biomes[GetIndex(bx, by, bz)] = paletteIndex;
    }

    public static void Serialize(BiomeStorage storage, ref BinaryWriter writer, bool disk = false)
    {
        int bitsPerBiome = ResolveBitsPerValue(storage.Palette.Count, true);

        writer.WriteUInt8((byte)(bitsPerBiome << 1));

        if (bitsPerBiome == 0)
        {
            int value = storage.Palette[0];
            if (disk)
            {
                writer.WriteInt32(value, littleEndian: true);
            }
            else
            {
                writer.WriteZigZag(value);
            }

            return;
        }

        int biomesPerWord = 32 / bitsPerBiome;
        int wordCount = (MaxSize + biomesPerWord - 1) / biomesPerWord;

        for (int w = 0; w < wordCount; w++)
        {
            int word = 0;
            for (int biome = 0; biome < biomesPerWord; biome++)
            {
                int index = w * biomesPerWord + biome;
                if (index >= MaxSize)
                {
                    break;
                }

                int state = storage.Biomes[index];
                word |= state << (biome * bitsPerBiome);
            }

            writer.WriteInt32(word, littleEndian: true);
        }

        if (disk)
        {
            writer.WriteInt32(storage.Palette.Count, littleEndian: true);
        }
        else
        {
            writer.WriteZigZag(storage.Palette.Count);
        }

        for (int i = 0; i < storage.Palette.Count; i++)
        {
            int state = storage.Palette[i];
            if (disk)
            {
                writer.WriteInt32(state, littleEndian: true);
            }
            else
            {
                writer.WriteZigZag(state);
            }
        }
    }

    public static BiomeStorage Deserialize(ref BinaryReader reader, bool disk = false)
    {
        byte paletteAndFlag = reader.ReadUInt8();
        int bitsPerBiome = paletteAndFlag >> 1;

        if (bitsPerBiome == 0x7F)
        {
            return new BiomeStorage();
        }

        if (bitsPerBiome == 0)
        {
            int value = disk ? reader.ReadInt32(littleEndian: true) : reader.ReadZigZag();
            return new BiomeStorage([value], new int[MaxSize]);
        }

        int biomesPerWord = 32 / bitsPerBiome;
        int wordCount = (MaxSize + biomesPerWord - 1) / biomesPerWord;

        int[] words = new int[wordCount];
        for (int i = 0; i < wordCount; i++)
        {
            words[i] = reader.ReadInt32(littleEndian: true);
        }

        int paletteSize = disk ? reader.ReadInt32(littleEndian: true) : reader.ReadZigZag();
        if (paletteSize <= 0)
        {
            throw new InvalidOperationException("Invalid biome palette size.");
        }

        List<int> palette = new(paletteSize);
        for (int i = 0; i < paletteSize; i++)
        {
            palette.Add(disk ? reader.ReadInt32(littleEndian: true) : reader.ReadZigZag());
        }

        int[] biomes = new int[MaxSize];
        int position = 0;
        int mask = (1 << bitsPerBiome) - 1;

        for (int w = 0; w < words.Length && position < MaxSize; w++)
        {
            int word = words[w];
            for (int biome = 0; biome < biomesPerWord && position < MaxSize; biome++, position++)
            {
                biomes[position] = (word >> (biome * bitsPerBiome)) & mask;
            }
        }

        return new BiomeStorage(palette, biomes);
    }

    private static int GetIndex(int bx, int by, int bz)
    {
        return ((bx & 0xF) << 8) | ((bz & 0xF) << 4) | (by & 0xF);
    }

    private static int ResolveBitsPerValue(int paletteLength, bool allowZero)
    {
        int bits = (int)Math.Ceiling(Math.Log2(Math.Max(1, paletteLength)));

        switch (bits)
        {
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







