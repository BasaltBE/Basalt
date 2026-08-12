#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EntityOffsetOption {
    public float EntityOffsetX;
    public float EntityOffsetY;
    public float EntityOffsetZ;

    public void Read(BinaryReader reader) {
        EntityOffsetX = reader.ReadF32(true);
        EntityOffsetY = reader.ReadF32(true);
        EntityOffsetZ = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(EntityOffsetX, true);
        writer.WriteF32(EntityOffsetY, true);
        writer.WriteF32(EntityOffsetZ, true);
    }
}
