#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MoveActorAbsoluteData {
    public ActorRuntimeID ActorRuntimeID = new();
    public byte Header;
    public Vec3 Position = new();
    public byte RotationX;
    public byte RotationY;
    public byte RotationYHead;

    public void Read(BinaryReader reader) {
        ActorRuntimeID.Read(reader);
        Header = reader.ReadUInt8();
        Position.Read(reader);
        RotationX = reader.ReadUInt8();
        RotationY = reader.ReadUInt8();
        RotationYHead = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        ActorRuntimeID.Write(writer);
        writer.WriteUInt8(Header);
        Position.Write(writer);
        writer.WriteUInt8(RotationX);
        writer.WriteUInt8(RotationY);
        writer.WriteUInt8(RotationYHead);
    }
}
