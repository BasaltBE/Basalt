using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CauldronUsed : LegacyTelemetryEventEventDataVariant {
    public uint ContentsColor;
    public short ContentsType;
    public short FillLevel;

    public void Read(BinaryReader reader) {
        ContentsColor = reader.ReadVarUInt();
        ContentsType = reader.ReadInt16(true);
        FillLevel = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(ContentsColor);
        writer.WriteInt16(ContentsType, true);
        writer.WriteInt16(FillLevel, true);
    }
}
