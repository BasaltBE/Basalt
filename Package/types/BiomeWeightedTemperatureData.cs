using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeWeightedTemperatureData {
    public int Temperature;
    public uint Weight;

    public void Read(BinaryReader reader) {
        Temperature = reader.ReadZigZag();
        Weight = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(Temperature);
        writer.WriteUInt32(Weight, true);
    }
}
