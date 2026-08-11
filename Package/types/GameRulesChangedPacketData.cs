using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class GameRulesChangedPacketData {
    public List<GameRule> RulesList = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        RulesList = new List<GameRule>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            GameRule item0 = default!;
            GameRule readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            RulesList.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)RulesList.Count));
        foreach (var item1 in RulesList) {
            item1.Write(writer);
        }
    }
}
