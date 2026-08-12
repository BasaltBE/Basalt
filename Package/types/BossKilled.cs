#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BossKilled : LegacyTelemetryEventEventDataVariant {
    public long BossActorID;
    public int PartySize;
    public int BossType;

    public void Read(BinaryReader reader) {
        BossActorID = reader.ReadZigZong();
        PartySize = reader.ReadZigZag();
        BossType = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(BossActorID);
        writer.WriteZigZag(PartySize);
        writer.WriteZigZag(BossType);
    }
}
