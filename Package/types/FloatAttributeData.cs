#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FloatAttributeData : EnvironmentAttributeDataAttributeVariant, EnvironmentAttributeDataFromAttributeVariant, EnvironmentAttributeDataToAttributeVariant {
    public float Value;
    public FloatAttributeOperation? Operation;
    public float? ConstraintMin;
    public float? ConstraintMax;

    public void Read(BinaryReader reader) {
        Value = reader.ReadF32(true);
        if (reader.ReadBool()) {
            Operation = (global::BedrockProtocol.Enums.FloatAttributeOperation)reader.ReadInt32(true);
        } else {
            Operation = default;
        }
        if (reader.ReadBool()) {
            ConstraintMin = reader.ReadF32(true);
        } else {
            ConstraintMin = default;
        }
        if (reader.ReadBool()) {
            ConstraintMax = reader.ReadF32(true);
        } else {
            ConstraintMax = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Value, true);
        writer.WriteBool(Operation is not null);
        if (Operation is { } optionalValue3) {
            writer.WriteInt32((int)optionalValue3, true);
        }
        writer.WriteBool(ConstraintMin is not null);
        if (ConstraintMin is { } optionalValue5) {
            writer.WriteF32(optionalValue5, true);
        }
        writer.WriteBool(ConstraintMax is not null);
        if (ConstraintMax is { } optionalValue7) {
            writer.WriteF32(optionalValue7, true);
        }
    }
}
