using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ResourcePackStackEntry : DataType
{
    /// <summary>
    /// The UUID of the resource pack.
    /// Which is used to identify packs.
    /// </summary>
    public Guid Uuid = Guid.Empty;

    /// <summary>
    /// The version of the resource pack.
    /// Which is used client side to check if it needs to request never version of an existing pack
    /// </summary>
    public string Version = "1.0.0";

    /// <summary>
    /// The sub pack name of the resource pack.
    /// </summary>
    public string SubPackName = string.Empty;

    public void Read(BinaryReader reader)
    {
        if (!Guid.TryParse(reader.ReadVarString(), out Guid uuid))
        {
            uuid = Guid.Empty;
        }

        Uuid = uuid;
        Version = reader.ReadVarString();
        SubPackName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Uuid.ToString());
        writer.WriteVarString(Version);
        writer.WriteVarString(SubPackName);
    }
}


