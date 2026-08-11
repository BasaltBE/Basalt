using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SlashCommand : LegacyTelemetryEventEventDataVariant {
    public int SuccessCount;
    public int ErrorCount;
    public string CommandName = string.Empty;
    public string ErrorList = string.Empty;

    public void Read(BinaryReader reader) {
        SuccessCount = reader.ReadZigZag();
        ErrorCount = reader.ReadZigZag();
        CommandName = reader.ReadVarString();
        ErrorList = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(SuccessCount);
        writer.WriteZigZag(ErrorCount);
        writer.WriteVarString(CommandName);
        writer.WriteVarString(ErrorList);
    }
}
