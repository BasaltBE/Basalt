#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EntityCommandTarget : CommandBlockUpdateTargetVariant {
    public ActorRuntimeID TargetRuntimeID = new();

    public void Read(BinaryReader reader) {
        TargetRuntimeID.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        TargetRuntimeID.Write(writer);
    }
}
