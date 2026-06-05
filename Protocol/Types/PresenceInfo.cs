using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PresenceInfo : DataType
{
    /// <summary>
    /// The name of the experience the player is in
    /// I think this is a dimension identifier, but I'm not sure
    /// Coming up with this after the new custom dimension stuff came out
    /// </summary>
    public string ExperienceName = string.Empty;

    /// <summary>
    /// The name of the world the player is in
    /// </summary>
    public string WorldName = string.Empty;

    public void Read(BinaryReader reader)
    {
        ExperienceName = reader.ReadVarString();
        WorldName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(ExperienceName);
        writer.WriteVarString(WorldName);
    }
}


