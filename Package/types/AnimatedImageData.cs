using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AnimatedImageData {
    public SkinImage SkinImage = new();
    public AnimatedTextureType AnimatedTextureType;
    public float Frames;
    public AnimationExpression AnimationExpression;

    public void Read(BinaryReader reader) {
        SkinImage.Read(reader);
        AnimatedTextureType = (global::BedrockProtocol.Enums.AnimatedTextureType)reader.ReadVarUInt();
        Frames = reader.ReadF32(true);
        AnimationExpression = (global::BedrockProtocol.Enums.AnimationExpression)reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        SkinImage.Write(writer);
        writer.WriteVarUInt((uint)AnimatedTextureType);
        writer.WriteF32(Frames, true);
        writer.WriteVarUInt((uint)AnimationExpression);
    }
}
