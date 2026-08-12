#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemUsed : LegacyTelemetryEventEventDataVariant {
    public short ItemId;
    public int ItemAux;
    public int UseMethod;
    public int Count;

    public void Read(BinaryReader reader) {
        ItemId = reader.ReadInt16(true);
        ItemAux = reader.ReadInt32(true);
        UseMethod = reader.ReadInt32(true);
        Count = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt16(ItemId, true);
        writer.WriteInt32(ItemAux, true);
        writer.WriteInt32(UseMethod, true);
        writer.WriteInt32(Count, true);
    }
}
