using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AddTimeMarkerData : SyncWorldClocksDataVariant {
    public ulong ClockId;
    public List<TimeMarkerData> TimeMarkers = [];

    public void Read(BinaryReader reader) {
        ClockId = reader.ReadVarULong();
        int count2 = checked((int)reader.ReadVarUInt());
        TimeMarkers = new List<TimeMarkerData>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            TimeMarkerData item2 = default!;
            TimeMarkerData readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            TimeMarkers.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(ClockId);
        writer.WriteVarUInt(checked((uint)TimeMarkers.Count));
        foreach (var item3 in TimeMarkers) {
            item3.Write(writer);
        }
    }
}
