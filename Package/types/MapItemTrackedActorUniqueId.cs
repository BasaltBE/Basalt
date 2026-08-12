#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MapItemTrackedActorUniqueId {
    public MapItemTrackedActorType Type;
    public ActorUniqueID? EntityID;
    public BlockPos? BlockPosition;

    public void Read(BinaryReader reader) {
        Type = (global::BedrockProtocol.Enums.MapItemTrackedActorType)reader.ReadInt32(true);
        if (reader.ReadBool()) {
            ActorUniqueID readValue2 = new();
            readValue2.Read(reader);
            EntityID = readValue2;
        } else {
            EntityID = default;
        }
        if (reader.ReadBool()) {
            BlockPos readValue4 = new();
            readValue4.Read(reader);
            BlockPosition = readValue4;
        } else {
            BlockPosition = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32((int)Type, true);
        writer.WriteBool(EntityID is not null);
        if (EntityID is { } optionalValue3) {
            optionalValue3.Write(writer);
        }
        writer.WriteBool(BlockPosition is not null);
        if (BlockPosition is { } optionalValue5) {
            optionalValue5.Write(writer);
        }
    }
}
