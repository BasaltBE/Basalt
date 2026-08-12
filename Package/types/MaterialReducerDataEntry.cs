#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MaterialReducerDataEntry {
    public int FromItemKey;
    public List<MaterialReducerEntryOutput> ItemIdsAndCounts = [];

    public void Read(BinaryReader reader) {
        FromItemKey = reader.ReadZigZag();
        int count2 = checked((int)reader.ReadVarUInt());
        ItemIdsAndCounts = new List<MaterialReducerEntryOutput>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            MaterialReducerEntryOutput item2 = default!;
            MaterialReducerEntryOutput readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            ItemIdsAndCounts.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(FromItemKey);
        writer.WriteVarUInt(checked((uint)ItemIdsAndCounts.Count));
        foreach (var item3 in ItemIdsAndCounts) {
            item3.Write(writer);
        }
    }
}
