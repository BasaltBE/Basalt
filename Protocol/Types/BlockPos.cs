using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct BlockPos : DataType
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public void Read(ref BinaryReader reader)
    {
        X = reader.ReadZigZag();
        Y = reader.ReadZigZag();
        Z = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(X);
        writer.WriteZigZag(Y);
        writer.WriteZigZag(Z);
    }
}

