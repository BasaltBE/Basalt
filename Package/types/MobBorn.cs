using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MobBorn : LegacyTelemetryEventEventDataVariant {
    public int BornBabyEntityType;
    public int BornBabyEntityVariant;
    public byte BornBabyColor;

    public void Read(BinaryReader reader) {
        BornBabyEntityType = reader.ReadZigZag();
        BornBabyEntityVariant = reader.ReadZigZag();
        BornBabyColor = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(BornBabyEntityType);
        writer.WriteZigZag(BornBabyEntityVariant);
        writer.WriteUInt8(BornBabyColor);
    }
}
