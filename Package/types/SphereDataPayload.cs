#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SphereDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public byte NumSegments;

    public void Read(BinaryReader reader) {
        NumSegments = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(NumSegments);
    }
}
