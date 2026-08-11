using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CylinderDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec2 RadiusX = new();
    public Vec2 RadiusZ = new();
    public float Height;
    public byte NumSegments;

    public void Read(BinaryReader reader) {
        RadiusX.Read(reader);
        RadiusZ.Read(reader);
        Height = reader.ReadF32(true);
        NumSegments = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        RadiusX.Write(writer);
        RadiusZ.Write(writer);
        writer.WriteF32(Height, true);
        writer.WriteUInt8(NumSegments);
    }
}
