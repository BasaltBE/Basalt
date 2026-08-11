using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeDefinitionData {
    public ushort Id;
    public float Temperature;
    public float Downfall;
    public float FoliageSnow;
    public float Depth;
    public float Scale;
    public int MapWaterColorARGB;
    public bool Rain;
    public BiomeTagsData Tags = new();
    public BiomeDefinitionChunkGenData ChunkGenData = new();

    public void Read(BinaryReader reader) {
        Id = reader.ReadUInt16(true);
        Temperature = reader.ReadF32(true);
        Downfall = reader.ReadF32(true);
        FoliageSnow = reader.ReadF32(true);
        Depth = reader.ReadF32(true);
        Scale = reader.ReadF32(true);
        MapWaterColorARGB = reader.ReadInt32(true);
        Rain = reader.ReadBool();
        Tags.Read(reader);
        ChunkGenData.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(Id, true);
        writer.WriteF32(Temperature, true);
        writer.WriteF32(Downfall, true);
        writer.WriteF32(FoliageSnow, true);
        writer.WriteF32(Depth, true);
        writer.WriteF32(Scale, true);
        writer.WriteInt32(MapWaterColorARGB, true);
        writer.WriteBool(Rain);
        Tags.Write(writer);
        ChunkGenData.Write(writer);
    }
}
