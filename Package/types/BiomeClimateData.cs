#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeClimateData {
    public float Temperature;
    public float Downfall;
    public float SnowAccumulationMin;
    public float SnowAccumulationMax;

    public void Read(BinaryReader reader) {
        Temperature = reader.ReadF32(true);
        Downfall = reader.ReadF32(true);
        SnowAccumulationMin = reader.ReadF32(true);
        SnowAccumulationMax = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Temperature, true);
        writer.WriteF32(Downfall, true);
        writer.WriteF32(SnowAccumulationMin, true);
        writer.WriteF32(SnowAccumulationMax, true);
    }
}
