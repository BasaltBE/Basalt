using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Respawn)]
public sealed record RespawnPacket : DataPacket
{
    public Vec3f Position;
    public RespawnState State;
    public ulong EntityRuntimeId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        State = (RespawnState)reader.ReadUInt8();
        EntityRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        Position.Write(writer);
        writer.WriteUInt8((byte)State);
        writer.WriteVarULong(EntityRuntimeId);
    }
}
