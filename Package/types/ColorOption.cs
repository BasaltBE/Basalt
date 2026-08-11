using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ColorOption {
    public float Red;
    public float Green;
    public float Blue;

    public void Read(BinaryReader reader) {
        Red = reader.ReadF32(true);
        Green = reader.ReadF32(true);
        Blue = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Red, true);
        writer.WriteF32(Green, true);
        writer.WriteF32(Blue, true);
    }
}
