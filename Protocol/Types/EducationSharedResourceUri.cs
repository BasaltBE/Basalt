using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class EducationSharedResourceUri : DataType
{
    /// <summary>
    /// The name of the button
    /// </summary>
    public string ButtonName = string.Empty;

    /// <summary>
    /// The URI that will be opened when the button is pressed
    /// Not really sure how it works tho
    /// </summary>
    public string LinkUri = string.Empty;

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

