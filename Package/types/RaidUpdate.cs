using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RaidUpdate : LegacyTelemetryEventEventDataVariant {
    public int CurrentWave;
    public int TotalWaves;
    public bool Success;

    public void Read(BinaryReader reader) {
        CurrentWave = reader.ReadZigZag();
        TotalWaves = reader.ReadZigZag();
        Success = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(CurrentWave);
        writer.WriteZigZag(TotalWaves);
        writer.WriteBool(Success);
    }
}
