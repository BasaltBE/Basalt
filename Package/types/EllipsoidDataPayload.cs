using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EllipsoidDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec3 Radii = new();
    public byte SegmentsPerAxis;

    public void Read(BinaryReader reader) {
        Radii.Read(reader);
        SegmentsPerAxis = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        Radii.Write(writer);
        writer.WriteUInt8(SegmentsPerAxis);
    }
}
