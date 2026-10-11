using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ServerTelemetryData : DataType {
    public string ServerId = string.Empty;
    public string ScenarioId = string.Empty;
    public string WorldId = string.Empty;
    public string OwnerId = string.Empty;

    public override void Read(ref BinaryReader reader) {
        ServerId = reader.ReadVarString();
        ScenarioId = reader.ReadVarString();
        WorldId = reader.ReadVarString();
        OwnerId = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(ServerId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(WorldId);
        writer.WriteVarString(OwnerId);
    }
}
