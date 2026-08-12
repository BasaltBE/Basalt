#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ProtocolShortTag {
    public short Data;

    public void Read(BinaryReader reader) {
        Data = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt16(Data, true);
    }
}
