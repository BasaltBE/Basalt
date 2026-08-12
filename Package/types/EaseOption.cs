#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EaseOption {
    public byte Type;
    public float Time;

    public void Read(BinaryReader reader) {
        Type = reader.ReadUInt8();
        Time = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Type);
        writer.WriteF32(Time, true);
    }
}
