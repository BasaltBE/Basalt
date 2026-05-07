using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record MovePlayerPacket : DataPacket
{
    public ulong RuntimeId { get; set; }
    public Vec3f Position { get; set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float HeadYaw { get; set; }
    public MoveMode Mode { get; set; }
    public bool OnGround { get; set; }
    public ulong RiddenRuntimeId { get; set; }
    public TeleportCause TeleportCause { get; set; }
    public int TeleportSourceEntityType { get; set; }
    public ulong Tick { get; set; }

    public override PacketId PacketId => PacketId.MovePlayer;

    public override void Deserialize(ref BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();
        Position.Read(ref reader);
        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        HeadYaw = reader.ReadF32(true);
        Mode = (MoveMode)reader.ReadUInt8();
        OnGround = reader.ReadBool();
        RiddenRuntimeId = reader.ReadVarULong();
        if (Mode == MoveMode.Teleport)
        {
            TeleportCause = (TeleportCause)reader.ReadInt32(true);
            TeleportSourceEntityType = reader.ReadInt32(true);
        }

        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        Position.Write(ref writer);
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        writer.WriteF32(HeadYaw, true);
        writer.WriteUInt8((byte)Mode);
        writer.WriteBool(OnGround);
        writer.WriteVarULong(RiddenRuntimeId);
        if (Mode == MoveMode.Teleport)
        {
            writer.WriteInt32((int)TeleportCause, true);
            writer.WriteInt32(TeleportSourceEntityType, true);
        }

        writer.WriteVarULong(Tick);
    }
}
