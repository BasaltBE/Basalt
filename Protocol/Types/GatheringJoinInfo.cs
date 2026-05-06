using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class GatheringJoinInfo
{
    public Guid ExperienceId { get; set; } = Guid.Empty;
    public string ExperienceName { get; set; } = string.Empty;
    public Guid ExperienceWorldId { get; set; } = Guid.Empty;
    public string ExperienceWorldName { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public Guid TargetId { get; set; } = Guid.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;

    public void Read(ref BinaryReader reader)
    {
        ExperienceId = UUID.Read(ref reader);
        ExperienceName = reader.ReadVarString();
        ExperienceWorldId = UUID.Read(ref reader);
        ExperienceWorldName = reader.ReadVarString();
        CreatorId = reader.ReadVarString();
        TargetId = UUID.Read(ref reader);
        ScenarioId = reader.ReadVarString();
        ServerId = reader.ReadVarString();
    }

    public void Write(ref BinaryWriter writer)
    {
        UUID.Write(ref writer, ExperienceId);
        writer.WriteVarString(ExperienceName);
        UUID.Write(ref writer, ExperienceWorldId);
        writer.WriteVarString(ExperienceWorldName);
        writer.WriteVarString(CreatorId);
        UUID.Write(ref writer, TargetId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(ServerId);
    }
}
