using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SyncWorldClockStateData {
    public ulong ClockId;
    public int Time;
    public bool IsPaused;

    public void Read(BinaryReader reader) {
        ClockId = reader.ReadVarULong();
        Time = reader.ReadZigZag();
        IsPaused = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(ClockId);
        writer.WriteZigZag(Time);
        writer.WriteBool(IsPaused);
    }
}
