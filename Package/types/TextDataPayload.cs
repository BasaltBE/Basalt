using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TextDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public string Text = string.Empty;
    public bool? UseRotation;
    public Color? BackgroundColor;
    public float? LineGapHeight;
    public bool? DepthTest;
    public bool? ShowBackface;
    public bool? ShowTextBackface;

    public void Read(BinaryReader reader) {
        Text = reader.ReadVarString();
        if (reader.ReadBool()) {
            UseRotation = reader.ReadBool();
        } else {
            UseRotation = default;
        }
        if (reader.ReadBool()) {
            Color readValue4 = new();
            readValue4.Read(reader);
            BackgroundColor = readValue4;
        } else {
            BackgroundColor = default;
        }
        if (reader.ReadBool()) {
            LineGapHeight = reader.ReadF32(true);
        } else {
            LineGapHeight = default;
        }
        if (reader.ReadBool()) {
            DepthTest = reader.ReadBool();
        } else {
            DepthTest = default;
        }
        if (reader.ReadBool()) {
            ShowBackface = reader.ReadBool();
        } else {
            ShowBackface = default;
        }
        if (reader.ReadBool()) {
            ShowTextBackface = reader.ReadBool();
        } else {
            ShowTextBackface = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Text);
        writer.WriteBool(UseRotation is not null);
        if (UseRotation is { } optionalValue3) {
            writer.WriteBool(optionalValue3);
        }
        writer.WriteBool(BackgroundColor is not null);
        if (BackgroundColor is { } optionalValue5) {
            optionalValue5.Write(writer);
        }
        writer.WriteBool(LineGapHeight is not null);
        if (LineGapHeight is { } optionalValue7) {
            writer.WriteF32(optionalValue7, true);
        }
        writer.WriteBool(DepthTest is not null);
        if (DepthTest is { } optionalValue9) {
            writer.WriteBool(optionalValue9);
        }
        writer.WriteBool(ShowBackface is not null);
        if (ShowBackface is { } optionalValue11) {
            writer.WriteBool(optionalValue11);
        }
        writer.WriteBool(ShowTextBackface is not null);
        if (ShowTextBackface is { } optionalValue13) {
            writer.WriteBool(optionalValue13);
        }
    }
}
