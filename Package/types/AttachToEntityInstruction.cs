using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AttachToEntityInstruction {
    public long EntityActorID;

    public void Read(BinaryReader reader) {
        EntityActorID = reader.ReadInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt64(EntityActorID, true);
    }
}
