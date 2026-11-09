using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SubChunkPos : DataType {
    public int X;
    public int Y;
    public int Z;

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadInt32(true);
        Y = reader.ReadInt32(true);
        Z = reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt32(X, true);
        writer.WriteInt32(Y, true);
        writer.WriteInt32(Z, true);
    }
}
