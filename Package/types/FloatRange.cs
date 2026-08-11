using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FloatRange {
    public float Min;
    public float Max;

    public void Read(BinaryReader reader) {
        Min = reader.ReadF32(true);
        Max = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Min, true);
        writer.WriteF32(Max, true);
    }
}
