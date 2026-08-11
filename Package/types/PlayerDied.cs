using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerDied : LegacyTelemetryEventEventDataVariant {
    public int InstigatorActorID;
    public int InstigatorMobVariant;
    public int DamageSource;
    public bool DiedInRaid;

    public void Read(BinaryReader reader) {
        InstigatorActorID = reader.ReadZigZag();
        InstigatorMobVariant = reader.ReadZigZag();
        DamageSource = reader.ReadZigZag();
        DiedInRaid = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(InstigatorActorID);
        writer.WriteZigZag(InstigatorMobVariant);
        writer.WriteZigZag(DamageSource);
        writer.WriteBool(DiedInRaid);
    }
}
