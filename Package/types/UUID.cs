using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UUID {
    public ulong MostSignificantBits;
    public ulong LeastSignificantBits;

    public void Read(BinaryReader reader) {
        MostSignificantBits = reader.ReadUInt64(true);
        LeastSignificantBits = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt64(MostSignificantBits, true);
        writer.WriteUInt64(LeastSignificantBits, true);
    }
}
