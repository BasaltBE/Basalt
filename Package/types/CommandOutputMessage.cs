#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CommandOutputMessage {
    public string MessageID = string.Empty;
    public bool Successful;
    public List<string> Parameters = [];

    public void Read(BinaryReader reader) {
        MessageID = reader.ReadVarString();
        Successful = reader.ReadBool();
        int count4 = checked((int)reader.ReadVarUInt());
        Parameters = new List<string>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string item4 = default!;
            item4 = reader.ReadVarString();
            Parameters.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(MessageID);
        writer.WriteBool(Successful);
        writer.WriteVarUInt(checked((uint)Parameters.Count));
        foreach (var item5 in Parameters) {
            writer.WriteVarString(item5);
        }
    }
}
