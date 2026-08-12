#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TimeOption {
    public float FadeInTime;
    public float HoldTime;
    public float FadeOutTime;

    public void Read(BinaryReader reader) {
        FadeInTime = reader.ReadF32(true);
        HoldTime = reader.ReadF32(true);
        FadeOutTime = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(FadeInTime, true);
        writer.WriteF32(HoldTime, true);
        writer.WriteF32(FadeOutTime, true);
    }
}
