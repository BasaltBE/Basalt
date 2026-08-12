#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DeathCauseMessageType {
    public string DeathCauseAttackName = string.Empty;
    public List<string> DeathCauseMessageList = [];

    public void Read(BinaryReader reader) {
        DeathCauseAttackName = reader.ReadVarString();
        int count2 = checked((int)reader.ReadVarUInt());
        DeathCauseMessageList = new List<string>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            string item2 = default!;
            item2 = reader.ReadVarString();
            DeathCauseMessageList.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(DeathCauseAttackName);
        writer.WriteVarUInt(checked((uint)DeathCauseMessageList.Count));
        foreach (var item3 in DeathCauseMessageList) {
            writer.WriteVarString(item3);
        }
    }
}
