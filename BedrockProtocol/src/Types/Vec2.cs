using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class Vec2 : DataType {
    public float X;
    public float Y;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
    }

    public override void Read(ref BinaryReader reader) {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
    }
}
