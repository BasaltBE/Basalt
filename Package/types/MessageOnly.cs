using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MessageOnly : TextBodyVariant {
    public TextPacketType MessageType;
    public string Message = string.Empty;

    public void Read(BinaryReader reader) {
        MessageType = (global::BedrockProtocol.Enums.TextPacketType)reader.ReadUInt8();
        Message = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)MessageType);
        writer.WriteVarString(Message);
    }
}
