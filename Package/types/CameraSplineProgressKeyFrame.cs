using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraSplineProgressKeyFrame {
    public float Progress;
    public float Time;
    public easing_function? Easing;

    public void Read(BinaryReader reader) {
        Progress = reader.ReadF32(true);
        Time = reader.ReadF32(true);
        if (reader.ReadBool()) {
            Easing = (global::BedrockProtocol.Enums.easing_function)reader.ReadInt32(true);
        } else {
            Easing = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Progress, true);
        writer.WriteF32(Time, true);
        writer.WriteBool(Easing is not null);
        if (Easing is { } optionalValue5) {
            writer.WriteInt32((int)optionalValue5, true);
        }
    }
}
