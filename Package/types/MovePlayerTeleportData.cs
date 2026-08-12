#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MovePlayerTeleportData {
    public int TeleportationCause;
    public int SourceActorType;

    public void Read(BinaryReader reader) {
        TeleportationCause = reader.ReadInt32(true);
        SourceActorType = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(TeleportationCause, true);
        writer.WriteInt32(SourceActorType, true);
    }
}
