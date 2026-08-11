using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class WorldClockData {
    public ulong Id;
    public string Name = string.Empty;
    public int Time;
    public bool IsPaused;
    public List<TimeMarkerData> TimeMarkers = [];

    public void Read(BinaryReader reader) {
        Id = reader.ReadVarULong();
        Name = reader.ReadVarString();
        Time = reader.ReadZigZag();
        IsPaused = reader.ReadBool();
        int count8 = checked((int)reader.ReadVarUInt());
        TimeMarkers = new List<TimeMarkerData>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            TimeMarkerData item8 = default!;
            TimeMarkerData readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            TimeMarkers.Add(item8);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(Id);
        writer.WriteVarString(Name);
        writer.WriteZigZag(Time);
        writer.WriteBool(IsPaused);
        writer.WriteVarUInt(checked((uint)TimeMarkers.Count));
        foreach (var item9 in TimeMarkers) {
            item9.Write(writer);
        }
    }
}
