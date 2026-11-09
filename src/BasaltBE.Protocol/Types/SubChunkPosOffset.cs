using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SubChunkPosOffset : DataType {
    public sbyte X;
    public sbyte Y;
    public sbyte Z;

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadInt8();
        Y = reader.ReadInt8();
        Z = reader.ReadInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt8(X);
        writer.WriteInt8(Y);
        writer.WriteInt8(Z);
    }
}
