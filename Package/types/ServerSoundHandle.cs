using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ServerSoundHandle {
    public ulong Value;

    public void Read(BinaryReader reader) {
        Value = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt64(Value, true);
    }
}
