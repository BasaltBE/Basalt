using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EntityDiagnosticTimingInfo {
    public string DisplayName = string.Empty;
    public string Entity = string.Empty;
    public ulong TimeInNS;
    public byte PercentOfTotal;

    public void Read(BinaryReader reader) {
        DisplayName = reader.ReadVarString();
        Entity = reader.ReadVarString();
        TimeInNS = reader.ReadUInt64(true);
        PercentOfTotal = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(DisplayName);
        writer.WriteVarString(Entity);
        writer.WriteUInt64(TimeInNS, true);
        writer.WriteUInt8(PercentOfTotal);
    }
}
