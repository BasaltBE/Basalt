#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Interaction : LegacyTelemetryEventEventDataVariant {
    public long InteractedEntityID;
    public InteractionType InteractionType;
    public int InteractionActorType;
    public int InteractionActorVariant;
    public byte InteractionActorColor;

    public void Read(BinaryReader reader) {
        InteractedEntityID = reader.ReadZigZong();
        InteractionType = (global::BedrockProtocol.Enums.InteractionType)reader.ReadUInt8();
        InteractionActorType = reader.ReadZigZag();
        InteractionActorVariant = reader.ReadZigZag();
        InteractionActorColor = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(InteractedEntityID);
        writer.WriteUInt8((byte)InteractionType);
        writer.WriteZigZag(InteractionActorType);
        writer.WriteZigZag(InteractionActorVariant);
        writer.WriteUInt8(InteractionActorColor);
    }
}
