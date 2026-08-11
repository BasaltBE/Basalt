using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BellUsed : LegacyTelemetryEventEventDataVariant {
    public short ItemId;

    public void Read(BinaryReader reader) {
        ItemId = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt16(ItemId, true);
    }
}
