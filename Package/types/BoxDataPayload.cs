#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BoxDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public Vec3 BoxBound = new();

    public void Read(BinaryReader reader) {
        BoxBound.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        BoxBound.Write(writer);
    }
}
