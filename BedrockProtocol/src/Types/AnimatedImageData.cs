using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AnimatedImageData : DataType {
    public SkinImage SkinImage = new();
    public AnimatedTextureType AnimatedTextureType;
    public float Frames;
    public AnimationExpression AnimationExpression;

    public override void Write(ref BinaryWriter writer) {
        SkinImage.Write(ref writer);
        writer.WriteVarUInt((uint)AnimatedTextureType);
        writer.WriteF32(Frames, true);
        writer.WriteVarUInt((uint)AnimationExpression);
    }

    public override void Read(ref BinaryReader reader) {
        SkinImage.Read(ref reader);
        AnimatedTextureType = (AnimatedTextureType)reader.ReadVarUInt();
        Frames = reader.ReadF32(true);
        AnimationExpression = (AnimationExpression)reader.ReadVarUInt();
    }
}
