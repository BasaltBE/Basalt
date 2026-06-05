using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class GatheringJoinInfo : DataType
{
    /// <summary>
    /// The UUID of the experience
    /// </summary>
    public Guid ExperienceId = Guid.Empty;

    /// <summary>
    /// The name of the experience
    /// </summary>
    public string ExperienceName = string.Empty;

    /// <summary>
    /// The UUID of the experience world
    /// </summary>
    public Guid ExperienceWorldId = Guid.Empty;

    /// <summary>
    /// The name of the experience world
    /// </summary>
    public string ExperienceWorldName = string.Empty;

    /// <summary>
    /// The Xbox Live ID of the creator of the experience
    /// </summary>
    public string CreatorId = string.Empty;

    /// <summary>
    /// The UUID of the target gathering to join
    /// </summary>
    public Guid TargetId = Guid.Empty;

    /// <summary>
    /// The ID of the scenario
    /// </summary>
    public string ScenarioId = string.Empty;

    /// <summary>
    /// The ID of the server
    /// </summary>
    public string ServerId = string.Empty;

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


