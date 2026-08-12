#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DisconnectPacketMessages {
    public string Message = string.Empty;
    public string FilteredMessage = string.Empty;

    public void Read(BinaryReader reader) {
        Message = reader.ReadVarString();
        FilteredMessage = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Message);
        writer.WriteVarString(FilteredMessage);
    }
}
