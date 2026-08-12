#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SyncedAttribute {
    public string AttributeName = string.Empty;
    public float MinValue;
    public float CurrentValue;
    public float MaxValue;

    public void Read(BinaryReader reader) {
        AttributeName = reader.ReadVarString();
        MinValue = reader.ReadF32(true);
        CurrentValue = reader.ReadF32(true);
        MaxValue = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(AttributeName);
        writer.WriteF32(MinValue, true);
        writer.WriteF32(CurrentValue, true);
        writer.WriteF32(MaxValue, true);
    }
}
