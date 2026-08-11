using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraSplineControlPoint {
    public Vec3 Position = new();

    public void Read(BinaryReader reader) {
        Position.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Position.Write(writer);
    }
}
