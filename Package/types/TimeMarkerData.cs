using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TimeMarkerData {
    public ulong Id;
    public string Name = string.Empty;
    public int Time;
    public int Period;

    public void Read(BinaryReader reader) {
        Id = reader.ReadVarULong();
        Name = reader.ReadVarString();
        Time = reader.ReadZigZag();
        Period = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(Id);
        writer.WriteVarString(Name);
        writer.WriteZigZag(Time);
        writer.WriteInt32(Period, true);
    }
}
