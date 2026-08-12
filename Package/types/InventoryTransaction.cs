#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class InventoryTransaction {
    public List<InventoryAction> Actions = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Actions = new List<InventoryAction>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            InventoryAction item0 = default!;
            InventoryAction readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Actions.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Actions.Count));
        foreach (var item1 in Actions) {
            item1.Write(writer);
        }
    }
}
