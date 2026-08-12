#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ActorRuntimeID {
    public ulong Value;

    public void Read(BinaryReader reader) {
        Value = reader.ReadVarULong();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(Value);
    }
}
