using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ResourcePackStackEntry
{
    public Guid Uuid { get; set; } = Guid.Empty;
    public string Version { get; set; } = "1.0.0";
    public string SubPackName { get; set; } = string.Empty;

    public void Read(ref BinaryReader reader)
    {
        if (!Guid.TryParse(reader.ReadVarString(), out Guid uuid))
        {
            uuid = Guid.Empty;
        }

        Uuid = uuid;
        Version = reader.ReadVarString();
        SubPackName = reader.ReadVarString();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Uuid.ToString());
        writer.WriteVarString(Version);
        writer.WriteVarString(SubPackName);
    }
}

