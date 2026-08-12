#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PortalCreated : LegacyTelemetryEventEventDataVariant {
    public int DimensionID;

    public void Read(BinaryReader reader) {
        DimensionID = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(DimensionID);
    }
}
