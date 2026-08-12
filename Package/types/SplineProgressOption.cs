#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SplineProgressOption {
    public float KeyFrameValue;
    public float KeyFrameTime;
    public easing_function KeyFrameEasingFunc;

    public void Read(BinaryReader reader) {
        KeyFrameValue = reader.ReadF32(true);
        KeyFrameTime = reader.ReadF32(true);
        KeyFrameEasingFunc = (global::BedrockProtocol.Enums.easing_function)reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(KeyFrameValue, true);
        writer.WriteF32(KeyFrameTime, true);
        writer.WriteInt32((int)KeyFrameEasingFunc, true);
    }
}
