using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct Vec3f(float x = 0, float y = 0, float z = 0) : DataType
{
    public static readonly Vec3f Zero = new(0, 0, 0);
    /// <summary>
    /// X coordinate of the vector.
    /// </summary>
    public float X = x;
    /// <summary>
    /// Y coordinate of the vector.
    /// </summary>
    public float Y = y;
    /// <summary>
    /// Z coordinate of the vector.
    /// </summary>
    public float Z = z;
    public void Read(BinaryReader reader)
    {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
        Z = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
        writer.WriteF32(Z, true);
    }
}

