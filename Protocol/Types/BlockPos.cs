using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct BlockPos : DataType
{   
    /// <summary>
    /// The X coordinate of the block position.
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// The Y coordinate of the block position.
    /// </summary>
    public int Y { get; set; }
    /// <summary>
    /// The Z coordinate of the block position.
    /// </summary>
    public int Z { get; set; }

    public void Read(BinaryReader reader)
    {
        X = reader.ReadZigZag();
        Y = reader.ReadZigZag();
        Z = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(X);
        writer.WriteZigZag(Y);
        writer.WriteZigZag(Z);
    }
}

