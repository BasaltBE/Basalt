using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class Vec3 : DataType {
    public float X;
    public float Y;
    public float Z;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
        writer.WriteF32(Z, true);
    }

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
        Z = reader.ReadF32(true);
    }
}
