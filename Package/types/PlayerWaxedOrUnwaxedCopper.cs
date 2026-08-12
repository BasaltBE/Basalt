#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerWaxedOrUnwaxedCopper : LegacyTelemetryEventEventDataVariant {
    public int PlayerWaxedOrUnwaxedCopperBlockID;

    public void Read(BinaryReader reader) {
        PlayerWaxedOrUnwaxedCopperBlockID = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PlayerWaxedOrUnwaxedCopperBlockID);
    }
}
