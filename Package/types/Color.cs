using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Color {
    public int Value;

    public void Read(BinaryReader reader) {
        Value = reader.ReadInt32(false);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(Value, false);
    }
}
