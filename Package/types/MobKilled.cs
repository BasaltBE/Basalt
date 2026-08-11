using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MobKilled : LegacyTelemetryEventEventDataVariant {
    public long InstigatorActorID;
    public long TargetActorID;
    public ActorType InstigatorSChildActorType;
    public int DamageSource;
    public int TradeTier;
    public string TraderName = string.Empty;

    public void Read(BinaryReader reader) {
        InstigatorActorID = reader.ReadZigZong();
        TargetActorID = reader.ReadZigZong();
        InstigatorSChildActorType = (global::BedrockProtocol.Enums.ActorType)reader.ReadZigZag();
        DamageSource = reader.ReadZigZag();
        TradeTier = reader.ReadZigZag();
        TraderName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(InstigatorActorID);
        writer.WriteZigZong(TargetActorID);
        writer.WriteZigZag((int)InstigatorSChildActorType);
        writer.WriteZigZag(DamageSource);
        writer.WriteZigZag(TradeTier);
        writer.WriteVarString(TraderName);
    }
}
