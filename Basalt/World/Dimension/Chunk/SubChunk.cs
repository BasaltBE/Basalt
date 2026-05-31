using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Server.World.Dimension.Chunk;

public sealed class SubChunk
{
    public byte Version { get; }
    public List<BlockStorage> Layers { get; }
    public BiomeStorage Biomes { get; set; }
    public sbyte? Index { get; set; }

    public SubChunk(byte version = 9, List<BlockStorage>? layers = null, BiomeStorage? biomes = null)
    {
        Version = version;
        Layers = layers ?? [];
        Biomes = biomes ?? new BiomeStorage();
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < Layers.Count; i++)
        {
            if (!Layers[i].IsEmpty())
            {
                return false;
            }
        }

        return true;
    }

    public BlockStorage GetLayer(int index)
    {
        while (Layers.Count <= index)
        {
            Layers.Add(new BlockStorage());
        }

        return Layers[index];
    }

    public int GetState(int bx, int by, int bz, int layer)
    {
        return (uint)layer < (uint)Layers.Count
            ? Layers[layer].GetState(bx, by, bz)
            : BlockStorage.Air;
    }

    public void SetState(int bx, int by, int bz, int state, int layer = 0)
    {
        GetLayer(layer).SetState(bx, by, bz, state);
    }

    public int GetBiome(int bx, int by, int bz)
    {
        return Biomes.GetBiome(bx, by, bz);
    }

    public void SetBiome(int bx, int by, int bz, int biome)
    {
        Biomes.SetBiome(bx, by, bz, biome);
    }

    public static void Serialize(SubChunk subChunk, BinaryWriter writer, bool nbt = false)
    {
        writer.WriteUInt8(subChunk.Version);
        writer.WriteUInt8(checked((byte)subChunk.Layers.Count));

        if (subChunk.Version == 9)
        {
            if (!subChunk.Index.HasValue)
            {
                throw new InvalidOperationException("SubChunk index is null for format version 9.");
            }

            writer.WriteInt8(subChunk.Index.Value);
        }

        for (int i = 0; i < subChunk.Layers.Count; i++)
        {
            BlockStorage.Serialize(subChunk.Layers[i], writer, nbt);
        }
    }

    public static SubChunk Deserialize(BinaryReader reader, bool nbt = false)
    {
        byte version = reader.ReadUInt8();
        byte count = reader.ReadUInt8();

        sbyte? index = null;
        if (version == 9)
        {
            index = reader.ReadInt8();
        }

        List<BlockStorage> layers = new(count);
        for (int i = 0; i < count; i++)
        {
            layers.Add(BlockStorage.Deserialize(ref reader, nbt));
        }

        SubChunk subChunk = new(version, layers)
        {
            Index = index
        };

        return subChunk;
    }

    public static SubChunk FromBuffer(ReadOnlySpan<byte> buffer, bool nbt = false)
    {
        int offset = 0;
        BinaryReader reader = new(buffer, ref offset);
        return Deserialize(reader, nbt);
    }
}







