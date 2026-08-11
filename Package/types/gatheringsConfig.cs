using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class gatheringsConfig {
    public UUID ExperienceId = new();
    public string ExperienceName = string.Empty;
    public UUID WorldId = new();
    public string WorldName = string.Empty;
    public string CreatorId = string.Empty;
    public UUID TargetId = new();
    public string ScenarioId = string.Empty;
    public string ServerId = string.Empty;

    public void Read(BinaryReader reader) {
        ExperienceId.Read(reader);
        ExperienceName = reader.ReadVarString();
        WorldId.Read(reader);
        WorldName = reader.ReadVarString();
        CreatorId = reader.ReadVarString();
        TargetId.Read(reader);
        ScenarioId = reader.ReadVarString();
        ServerId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        ExperienceId.Write(writer);
        writer.WriteVarString(ExperienceName);
        WorldId.Write(writer);
        writer.WriteVarString(WorldName);
        writer.WriteVarString(CreatorId);
        TargetId.Write(writer);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(ServerId);
    }
}
