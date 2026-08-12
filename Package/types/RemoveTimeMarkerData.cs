#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RemoveTimeMarkerData : SyncWorldClocksDataVariant {
    public ulong ClockId;
    public List<ulong> TimeMarkerIds = [];

    public void Read(BinaryReader reader) {
        ClockId = reader.ReadVarULong();
        int count2 = checked((int)reader.ReadVarUInt());
        TimeMarkerIds = new List<ulong>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            ulong item2 = default!;
            item2 = reader.ReadUInt64(true);
            TimeMarkerIds.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(ClockId);
        writer.WriteVarUInt(checked((uint)TimeMarkerIds.Count));
        foreach (var item3 in TimeMarkerIds) {
            writer.WriteUInt64(item3, true);
        }
    }
}
