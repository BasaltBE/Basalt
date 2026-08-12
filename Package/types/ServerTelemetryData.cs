#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ServerTelemetryData {
    public string ServerId = string.Empty;
    public string ScenarioId = string.Empty;
    public string WorldId = string.Empty;
    public string OwnerId = string.Empty;

    public void Read(BinaryReader reader) {
        ServerId = reader.ReadVarString();
        ScenarioId = reader.ReadVarString();
        WorldId = reader.ReadVarString();
        OwnerId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(ServerId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(WorldId);
        writer.WriteVarString(OwnerId);
    }
}
