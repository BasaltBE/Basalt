#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class WorldPosition {
    public Vec3 Position = new();
    public DimensionType DimensionType = new();

    public void Read(BinaryReader reader) {
        Position.Read(reader);
        DimensionType.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Position.Write(writer);
        DimensionType.Write(writer);
    }
}
