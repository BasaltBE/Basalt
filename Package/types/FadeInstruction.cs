#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FadeInstruction {
    public TimeOption Time = new();
    public ColorOption Color = new();

    public void Read(BinaryReader reader) {
        Time.Read(reader);
        Color.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Time.Write(writer);
        Color.Write(writer);
    }
}
