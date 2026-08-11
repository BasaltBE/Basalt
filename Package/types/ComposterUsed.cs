using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ComposterUsed : LegacyTelemetryEventEventDataVariant {
    public POIBlockInteractionType BlockInteractionType;
    public short ItemId;

    public void Read(BinaryReader reader) {
        BlockInteractionType = (global::BedrockProtocol.Enums.POIBlockInteractionType)reader.ReadUInt8();
        ItemId = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)BlockInteractionType);
        writer.WriteInt16(ItemId, true);
    }
}
