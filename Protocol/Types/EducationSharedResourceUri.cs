using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class EducationSharedResourceUri : DataType
{
    public string ButtonName { get; set; } = string.Empty;
    public string LinkUri { get; set; } = string.Empty;

    public void Read(BinaryReader reader)
    {
        ButtonName = reader.ReadVarString();
        LinkUri = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(ButtonName);
        writer.WriteVarString(LinkUri);
    }
}

