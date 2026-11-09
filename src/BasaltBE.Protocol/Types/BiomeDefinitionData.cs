using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class BiomeDefinitionData : DataType {
    public ushort Id;
    public float Temperature;
    public float Downfall;
    public float FoliageSnow;
    public float Depth;
    public float Scale;
    public int MapWaterColorArgb;
    public bool Rain;
    public ushort[]? Tags;

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadUInt16(true);
        Temperature = BitConverter.Int32BitsToSingle(reader.ReadInt32(true));
        Downfall = BitConverter.Int32BitsToSingle(reader.ReadInt32(true));
        FoliageSnow = BitConverter.Int32BitsToSingle(reader.ReadInt32(true));
        Depth = BitConverter.Int32BitsToSingle(reader.ReadInt32(true));
        Scale = BitConverter.Int32BitsToSingle(reader.ReadInt32(true));
        MapWaterColorArgb = reader.ReadInt32(true);
        Rain = reader.ReadBool();
        if (reader.ReadBool()) {
            uint tagCount = reader.ReadVarUInt();
            if (tagCount > ushort.MaxValue || tagCount > (uint)(reader.Remaining / sizeof(ushort)))
                throw new InvalidDataException("Biome tag count exceeds the remaining packet data.");

            Tags = new ushort[checked((int)tagCount)];
            for (int i = 0; i < Tags.Length; i++)
                Tags[i] = reader.ReadUInt16(true);
        }

        if (reader.ReadBool())
            throw new InvalidDataException("Biome chunk generation data is not supported.");
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt16(Id, true);
        writer.WriteInt32(BitConverter.SingleToInt32Bits(Temperature), true);
        writer.WriteInt32(BitConverter.SingleToInt32Bits(Downfall), true);
        writer.WriteInt32(BitConverter.SingleToInt32Bits(FoliageSnow), true);
        writer.WriteInt32(BitConverter.SingleToInt32Bits(Depth), true);
        writer.WriteInt32(BitConverter.SingleToInt32Bits(Scale), true);
        writer.WriteInt32(MapWaterColorArgb, true);
        writer.WriteBool(Rain);
        writer.WriteBool(Tags is not null);
        if (Tags is { } tags) {
            writer.WriteVarUInt((uint)tags.Length);
            foreach (ushort tag in tags)
                writer.WriteUInt16(tag, true);
        }
        writer.WriteBool(false);
    }
}
