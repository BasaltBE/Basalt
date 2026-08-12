#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SystemDiagnosticTimingInfo {
    public string DisplayName = string.Empty;
    public ulong SystemIndex;
    public ulong TimeInNS;
    public byte PercentOfTotal;

    public void Read(BinaryReader reader) {
        DisplayName = reader.ReadVarString();
        SystemIndex = reader.ReadUInt64(true);
        TimeInNS = reader.ReadUInt64(true);
        PercentOfTotal = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(DisplayName);
        writer.WriteUInt64(SystemIndex, true);
        writer.WriteUInt64(TimeInNS, true);
        writer.WriteUInt8(PercentOfTotal);
    }
}
