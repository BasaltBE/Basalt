#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MapDecoration {
    public MapDecorationType ImageType;
    public byte Rotation;
    public byte X;
    public byte Y;
    public string Label = string.Empty;
    public Color Color = new();

    public void Read(BinaryReader reader) {
        ImageType = (MapDecorationType)reader.ReadInt8();
        Rotation = reader.ReadUInt8();
        X = reader.ReadUInt8();
        Y = reader.ReadUInt8();
        Label = reader.ReadVarString();
        Color.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt8((sbyte)ImageType);
        writer.WriteUInt8(Rotation);
        writer.WriteUInt8(X);
        writer.WriteUInt8(Y);
        writer.WriteVarString(Label);
        Color.Write(writer);
    }
}
