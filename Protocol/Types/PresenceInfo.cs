using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PresenceInfo
{
    public string ExperienceName { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;

    public void Read(ref BinaryReader reader)
    {
        ExperienceName = reader.ReadVarString();
        WorldName = reader.ReadVarString();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(ExperienceName);
        writer.WriteVarString(WorldName);
    }
}
