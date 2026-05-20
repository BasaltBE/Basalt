using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class GatheringJoinInfo : DataType
{
    public Guid ExperienceId { get; set; } = Guid.Empty;
    public string ExperienceName { get; set; } = string.Empty;
    public Guid ExperienceWorldId { get; set; } = Guid.Empty;
    public string ExperienceWorldName { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public Guid TargetId { get; set; } = Guid.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;

    public void Read(BinaryReader reader)
    {
        ExperienceId = UUID.Read(reader);
        ExperienceName = reader.ReadVarString();
        ExperienceWorldId = UUID.Read(reader);
        ExperienceWorldName = reader.ReadVarString();
        CreatorId = reader.ReadVarString();
        TargetId = UUID.Read(reader);
        ScenarioId = reader.ReadVarString();
        ServerId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer)
    {
        UUID.Write(writer, ExperienceId);
        writer.WriteVarString(ExperienceName);
        UUID.Write(writer, ExperienceWorldId);
        writer.WriteVarString(ExperienceWorldName);
        writer.WriteVarString(CreatorId);
        UUID.Write(writer, TargetId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(ServerId);
    }
}


