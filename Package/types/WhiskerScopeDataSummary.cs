#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class WhiskerScopeDataSummary {
    public string Label = string.Empty;
    public string Indentation = string.Empty;
    public ulong TotalHighCostNS;
    public ulong TotalMidCostNS;
    public ulong TotalLowCostNS;

    public void Read(BinaryReader reader) {
        Label = reader.ReadVarString();
        Indentation = reader.ReadVarString();
        TotalHighCostNS = reader.ReadUInt64(true);
        TotalMidCostNS = reader.ReadUInt64(true);
        TotalLowCostNS = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Label);
        writer.WriteVarString(Indentation);
        writer.WriteUInt64(TotalHighCostNS, true);
        writer.WriteUInt64(TotalMidCostNS, true);
        writer.WriteUInt64(TotalLowCostNS, true);
    }
}
