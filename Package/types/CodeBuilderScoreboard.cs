using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CodeBuilderScoreboard : LegacyTelemetryEventEventDataVariant {
    public string ObjectiveName = string.Empty;
    public int Score;

    public void Read(BinaryReader reader) {
        ObjectiveName = reader.ReadVarString();
        Score = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(ObjectiveName);
        writer.WriteZigZag(Score);
    }
}
