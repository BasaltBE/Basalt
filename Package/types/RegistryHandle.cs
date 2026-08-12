#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RegistryHandle {
    public ushort Value;

    public void Read(BinaryReader reader) {
        Value = reader.ReadUInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(Value, true);
    }
}
