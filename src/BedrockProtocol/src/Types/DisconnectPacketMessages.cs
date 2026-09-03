using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class DisconnectPacketMessages : DataType {
    public string Message = string.Empty;
    public string FilteredMessage = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Message);
        writer.WriteVarString(FilteredMessage);
    }

    public override void Read(ref BinaryReader reader) {
        Message = reader.ReadVarString();
        FilteredMessage = reader.ReadVarString();
    }
}
