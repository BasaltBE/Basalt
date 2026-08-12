#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ColorAttributeData : EnvironmentAttributeDataAttributeVariant, EnvironmentAttributeDataFromAttributeVariant, EnvironmentAttributeDataToAttributeVariant {
    public Color255RGBA Value = null!;
    public ColorAttributeOperation? Operation;

    public void Read(BinaryReader reader) {
        Color255RGBA readUnion0 = new();
        readUnion0.Read(reader);
        Value = readUnion0;
        if (reader.ReadBool()) {
            Operation = (global::BedrockProtocol.Enums.ColorAttributeOperation)reader.ReadInt32(true);
        } else {
            Operation = default;
        }
    }

    public void Write(BinaryWriter writer) {
        Value.Write(writer);
        writer.WriteBool(Operation is not null);
        if (Operation is { } optionalValue3) {
            writer.WriteInt32((int)optionalValue3, true);
        }
    }
}
