using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PiglinBarter : LegacyTelemetryEventEventDataVariant {
    public int ItemId;
    public bool WasTargetingBarteringPlayer;

    public void Read(BinaryReader reader) {
        ItemId = reader.ReadZigZag();
        WasTargetingBarteringPlayer = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(ItemId);
        writer.WriteBool(WasTargetingBarteringPlayer);
    }
}
