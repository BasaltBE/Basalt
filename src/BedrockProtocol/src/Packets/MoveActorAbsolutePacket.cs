using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(18)]
public sealed class MoveActorAbsolutePacket : DataPacket {
    public ulong ActorRuntimeId;
    public byte Header;
    public Vec3 Position = new();
    public byte RotationX;
    public byte RotationY;
    public byte RotationYHead;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteUInt8(Header);
        Position.Write(ref writer);
        writer.WriteUInt8(RotationX);
        writer.WriteUInt8(RotationY);
        writer.WriteUInt8(RotationYHead);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        Header = reader.ReadUInt8();
        Position.Read(ref reader);
        RotationX = reader.ReadUInt8();
        RotationY = reader.ReadUInt8();
        RotationYHead = reader.ReadUInt8();
    }
}
