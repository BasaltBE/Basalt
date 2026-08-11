using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PyramidDataPayload : PrimitiveShapeDataPayloadExtraShapeDataVariant {
    public float Width;
    public float? Depth;
    public float Height;

    public void Read(BinaryReader reader) {
        Width = reader.ReadF32(true);
        if (reader.ReadBool()) {
            Depth = reader.ReadF32(true);
        } else {
            Depth = default;
        }
        Height = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Width, true);
        writer.WriteBool(Depth is not null);
        if (Depth is { } optionalValue3) {
            writer.WriteF32(optionalValue3, true);
        }
        writer.WriteF32(Height, true);
    }
}
