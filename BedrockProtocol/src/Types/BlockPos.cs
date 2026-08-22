using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class BlockPos : DataType {
    public int X;
    public int Y;
    public int Z;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(X);
        writer.WriteZigZag(Y);
        writer.WriteZigZag(Z);
    }

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadZigZag();
        Y = reader.ReadZigZag();
        Z = reader.ReadZigZag();
    }
}
