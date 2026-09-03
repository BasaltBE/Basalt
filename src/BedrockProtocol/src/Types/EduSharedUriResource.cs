using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class EduSharedUriResource : DataType {
    public string ButtonName = string.Empty;
    public string LinkUri = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(ButtonName);
        writer.WriteVarString(LinkUri);
    }

    public override void Read(ref BinaryReader reader) {
        ButtonName = reader.ReadVarString();
        LinkUri = reader.ReadVarString();
    }
}
