using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PortalUsed : LegacyTelemetryEventEventDataVariant {
    public int SourceDimensionID;
    public int TargetDimensionID;

    public void Read(BinaryReader reader) {
        SourceDimensionID = reader.ReadZigZag();
        TargetDimensionID = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(SourceDimensionID);
        writer.WriteZigZag(TargetDimensionID);
    }
}
