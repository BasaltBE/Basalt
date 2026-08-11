using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class LineDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec3 LineEndLocation = new();

    public void Read(BinaryReader reader) {
        LineEndLocation.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        LineEndLocation.Write(writer);
    }
}
