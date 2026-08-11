using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeStringList {
    public List<string> Strings = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Strings = new List<string>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            string item0 = default!;
            item0 = reader.ReadVarString();
            Strings.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Strings.Count));
        foreach (var item1 in Strings) {
            writer.WriteVarString(item1);
        }
    }
}
