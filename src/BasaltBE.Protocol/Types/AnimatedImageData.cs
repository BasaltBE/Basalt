using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AnimatedImageData : DataType {
    public SkinImage Image = new();
    public PersonaAnimatedTextureType TextureType;
    public float Frames;
    public PersonaAnimationExpression Expression;

    public override void Read(ref BinaryReader reader) {
        Image.Read(ref reader);
        TextureType = (PersonaAnimatedTextureType)reader.ReadVarUInt();
        Frames = reader.ReadF32(true);
        Expression = (PersonaAnimationExpression)reader.ReadVarUInt();
    }

    public override void Write(ref BinaryWriter writer) {
        Image.Write(ref writer);
        writer.WriteVarUInt((uint)TextureType);
        writer.WriteF32(Frames, true);
        writer.WriteVarUInt((uint)Expression);
    }
}
