using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct Vec3f : DataType
{   
    /// <summary>
    /// X coordinate of the vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate of the vector.
    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// Z coordinate of the vector.
    /// </summary>
    public float Z { get; set; }

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

