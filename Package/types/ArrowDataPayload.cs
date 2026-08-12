#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ArrowDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec3 ArrowEndLocation = new();
    public float ArrowHeadLength;
    public float ArrowHeadRadius;
    public byte NumSegments;

    public void Read(BinaryReader reader) {
        ArrowEndLocation.Read(reader);
        ArrowHeadLength = reader.ReadF32(true);
        ArrowHeadRadius = reader.ReadF32(true);
        NumSegments = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        ArrowEndLocation.Write(writer);
        writer.WriteF32(ArrowHeadLength, true);
        writer.WriteF32(ArrowHeadRadius, true);
        writer.WriteUInt8(NumSegments);
    }
}
