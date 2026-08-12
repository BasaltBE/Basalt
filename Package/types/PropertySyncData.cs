#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PropertySyncData {
    public List<PropertySyncIntEntry> IntEntriesList = [];
    public List<PropertySyncFloatEntry> FloatEntriesList = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        IntEntriesList = new List<PropertySyncIntEntry>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            PropertySyncIntEntry item0 = default!;
            PropertySyncIntEntry readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            IntEntriesList.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        FloatEntriesList = new List<PropertySyncFloatEntry>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            PropertySyncFloatEntry item2 = default!;
            PropertySyncFloatEntry readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            FloatEntriesList.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)IntEntriesList.Count));
        foreach (var item1 in IntEntriesList) {
            item1.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)FloatEntriesList.Count));
        foreach (var item3 in FloatEntriesList) {
            item3.Write(writer);
        }
    }
}
