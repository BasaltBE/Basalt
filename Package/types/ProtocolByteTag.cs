#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ProtocolByteTag {
    public byte Data;

    public void Read(BinaryReader reader) {
        Data = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Data);
    }
}
