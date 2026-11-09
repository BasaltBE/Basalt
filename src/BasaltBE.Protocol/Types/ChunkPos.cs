using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ChunkPos : DataType {
    public int X;
    public int Z;

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadZigZag();
        Z = reader.ReadZigZag();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(X);
        writer.WriteZigZag(Z);
    }
}
