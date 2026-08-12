#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SplineRotationOption {
    public Vec3 KeyFrameValue = new();
    public float KeyFrameTime;
    public easing_function KeyFrameEasingFunc;

    public void Read(BinaryReader reader) {
        KeyFrameValue.Read(reader);
        KeyFrameTime = reader.ReadF32(true);
        KeyFrameEasingFunc = (global::BedrockProtocol.Enums.easing_function)reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        KeyFrameValue.Write(writer);
        writer.WriteF32(KeyFrameTime, true);
        writer.WriteInt32((int)KeyFrameEasingFunc, true);
    }
}
