#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketEnumData {
    public string Name = string.Empty;
    public List<uint> Values = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        int count2 = checked((int)reader.ReadVarUInt());
        Values = new List<uint>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            uint item2 = default!;
            item2 = reader.ReadUInt32(true);
            Values.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt(checked((uint)Values.Count));
        foreach (var item3 in Values) {
            writer.WriteUInt32(item3, true);
        }
    }
}
