using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SyncStateData : SyncWorldClocksDataVariant {
    public List<SyncWorldClockStateData> ClockData = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        ClockData = new List<SyncWorldClockStateData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            SyncWorldClockStateData item0 = default!;
            SyncWorldClockStateData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            ClockData.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)ClockData.Count));
        foreach (var item1 in ClockData) {
            item1.Write(writer);
        }
    }
}
