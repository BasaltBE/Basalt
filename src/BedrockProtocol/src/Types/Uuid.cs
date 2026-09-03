using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class Uuid : DataType {
    public ulong MostSignificantBits;
    public ulong LeastSignificantBits;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt64(MostSignificantBits, true);
        writer.WriteUInt64(LeastSignificantBits, true);
    }

    public override void Read(ref BinaryReader reader) {
        MostSignificantBits = reader.ReadUInt64(true);
        LeastSignificantBits = reader.ReadUInt64(true);
    }
}
