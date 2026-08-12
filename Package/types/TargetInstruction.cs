#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TargetInstruction {
    public Vec3 TargetCenterOffset = new();
    public long TargetActorID;

    public void Read(BinaryReader reader) {
        TargetCenterOffset.Read(reader);
        TargetActorID = reader.ReadInt64(true);
    }

    public void Write(BinaryWriter writer) {
        TargetCenterOffset.Write(writer);
        writer.WriteInt64(TargetActorID, true);
    }
}
