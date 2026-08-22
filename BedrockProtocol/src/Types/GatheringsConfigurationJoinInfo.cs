using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class GatheringsConfigurationJoinInfo : DataType {
    public Uuid ExperienceId = new();
    public string ExperienceName = string.Empty;
    public Uuid? WorldId;
    public string? WorldName;
    public string CreatorId = string.Empty;
    public Uuid? TargetId;
    public string? ScenarioId;
    public string? ServerId;

    public override void Write(ref BinaryWriter writer) {
        ExperienceId.Write(ref writer);
        writer.WriteVarString(ExperienceName);
        writer.WriteBool(WorldId is not null);
        if (WorldId is not null) WorldId.Write(ref writer);
        writer.WriteBool(WorldName is not null);
        if (WorldName is not null) writer.WriteVarString(WorldName);
        writer.WriteVarString(CreatorId);
        writer.WriteBool(TargetId is not null);
        if (TargetId is not null) TargetId.Write(ref writer);
        writer.WriteBool(ScenarioId is not null);
        if (ScenarioId is not null) writer.WriteVarString(ScenarioId);
        writer.WriteBool(ServerId is not null);
        if (ServerId is not null) writer.WriteVarString(ServerId);
    }

    public override void Read(ref BinaryReader reader) {
        ExperienceId.Read(ref reader);
        ExperienceName = reader.ReadVarString();
        WorldId = reader.ReadBool() ? new Uuid() : null;
        if (WorldId is not null) WorldId.Read(ref reader);
        WorldName = reader.ReadBool() ? reader.ReadVarString() : null;
        CreatorId = reader.ReadVarString();
        TargetId = reader.ReadBool() ? new Uuid() : null;
        if (TargetId is not null) TargetId.Read(ref reader);
        ScenarioId = reader.ReadBool() ? reader.ReadVarString() : null;
        ServerId = reader.ReadBool() ? reader.ReadVarString() : null;
    }
}
