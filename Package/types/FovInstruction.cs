#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FovInstruction {
    public float FieldOfView;
    public float FOVEaseTime;
    public easing_function FOVEaseType;
    public bool FieldOfViewClear;

    public void Read(BinaryReader reader) {
        FieldOfView = reader.ReadF32(true);
        FOVEaseTime = reader.ReadF32(true);
        FOVEaseType = (global::BedrockProtocol.Enums.easing_function)reader.ReadInt32(true);
        FieldOfViewClear = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(FieldOfView, true);
        writer.WriteF32(FOVEaseTime, true);
        writer.WriteInt32((int)FOVEaseType, true);
        writer.WriteBool(FieldOfViewClear);
    }
}
