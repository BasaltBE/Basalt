#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ConeDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec2 Radii = new();
    public float Height;
    public byte NumSegments;

    public void Read(BinaryReader reader) {
        Radii.Read(reader);
        Height = reader.ReadF32(true);
        NumSegments = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        Radii.Write(writer);
        writer.WriteF32(Height, true);
        writer.WriteUInt8(NumSegments);
    }
}
