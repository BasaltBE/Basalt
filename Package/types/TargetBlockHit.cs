#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TargetBlockHit : LegacyTelemetryEventEventDataVariant {
    public int RedstoneLevel;

    public void Read(BinaryReader reader) {
        RedstoneLevel = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(RedstoneLevel);
    }
}
