using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MessageAndParams : TextBodyVariant {
    public TextPacketType MessageType;
    public string Message = string.Empty;
    public List<string> ParameterList = [];

    public void Read(BinaryReader reader) {
        MessageType = (global::BedrockProtocol.Enums.TextPacketType)reader.ReadUInt8();
        Message = reader.ReadVarString();
        int count4 = checked((int)reader.ReadVarUInt());
        ParameterList = new List<string>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string item4 = default!;
            item4 = reader.ReadVarString();
            ParameterList.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)MessageType);
        writer.WriteVarString(Message);
        writer.WriteVarUInt(checked((uint)ParameterList.Count));
        foreach (var item5 in ParameterList) {
            writer.WriteVarString(item5);
        }
    }
}
