using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeElementData {
    public float NoiseFreqScale;
    public float NoiseLowerBound;
    public float NoiseUpperBound;
    public int HeightMinType;
    public ushort HeightMin;
    public int HeightMaxType;
    public ushort HeightMax;
    public BiomeSurfaceMaterialData AdjustedMaterials = new();

    public void Read(BinaryReader reader) {
        NoiseFreqScale = reader.ReadF32(true);
        NoiseLowerBound = reader.ReadF32(true);
        NoiseUpperBound = reader.ReadF32(true);
        HeightMinType = reader.ReadZigZag();
        HeightMin = reader.ReadUInt16(true);
        HeightMaxType = reader.ReadZigZag();
        HeightMax = reader.ReadUInt16(true);
        AdjustedMaterials.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(NoiseFreqScale, true);
        writer.WriteF32(NoiseLowerBound, true);
        writer.WriteF32(NoiseUpperBound, true);
        writer.WriteZigZag(HeightMinType);
        writer.WriteUInt16(HeightMin, true);
        writer.WriteZigZag(HeightMaxType);
        writer.WriteUInt16(HeightMax, true);
        AdjustedMaterials.Write(writer);
    }
}
