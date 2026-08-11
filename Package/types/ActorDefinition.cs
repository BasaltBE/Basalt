using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ActorDefinition : LegacyTelemetryEventEventDataVariant {
    public string EventName = string.Empty;

    public void Read(BinaryReader reader) {
        EventName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(EventName);
    }
}
