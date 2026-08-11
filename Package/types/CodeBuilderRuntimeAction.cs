using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CodeBuilderRuntimeAction : LegacyTelemetryEventEventDataVariant {
    public string Value = string.Empty;

    public void Read(BinaryReader reader) {
        Value = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Value);
    }
}
