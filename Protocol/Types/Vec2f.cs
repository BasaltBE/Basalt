using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct Vec2f : DataType
{
    public float X { get; set; }
    public float Y { get; set; }

    public void Read(ref BinaryReader reader)
    {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
    }
}
