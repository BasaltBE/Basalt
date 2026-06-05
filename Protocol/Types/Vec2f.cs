using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct Vec2f : DataType
{
    /// <summary>
    /// X coordinate of the vector.
    /// </summary>
    public float X;
    /// <summary>
    /// Y coordinate of the vector.
    /// </summary>
    public float Y;
    public void Read(BinaryReader reader)
    {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
    }
}
