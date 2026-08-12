#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BoolAttributeData : EnvironmentAttributeDataAttributeVariant, EnvironmentAttributeDataFromAttributeVariant, EnvironmentAttributeDataToAttributeVariant {
    public bool Value;
    public BoolAttributeOperation? Operation;

    public void Read(BinaryReader reader) {
        Value = reader.ReadBool();
        if (reader.ReadBool()) {
            Operation = (global::BedrockProtocol.Enums.BoolAttributeOperation)reader.ReadInt32(true);
        } else {
            Operation = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(Value);
        writer.WriteBool(Operation is not null);
        if (Operation is { } optionalValue3) {
            writer.WriteInt32((int)optionalValue3, true);
        }
    }
}
