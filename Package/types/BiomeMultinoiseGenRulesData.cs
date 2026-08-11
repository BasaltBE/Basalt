using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeMultinoiseGenRulesData {
    public float Temperature;
    public float Humidity;
    public float Altitude;
    public float Weirdness;
    public float Weight;

    public void Read(BinaryReader reader) {
        Temperature = reader.ReadF32(true);
        Humidity = reader.ReadF32(true);
        Altitude = reader.ReadF32(true);
        Weirdness = reader.ReadF32(true);
        Weight = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Temperature, true);
        writer.WriteF32(Humidity, true);
        writer.WriteF32(Altitude, true);
        writer.WriteF32(Weirdness, true);
        writer.WriteF32(Weight, true);
    }
}
