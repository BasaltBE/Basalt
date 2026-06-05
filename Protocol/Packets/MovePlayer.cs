using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.MovePlayer)]
public sealed record MovePlayerPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the moving actor.
    /// </summary>
    public ulong RuntimeId;

    /// <summary>
    /// Actor position in world coordinates.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Camera pitch angle.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Body yaw angle.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Head yaw angle.
    /// </summary>
    public float HeadYaw;

    /// <summary>
    /// Movement mode.
    /// </summary>
    public MoveMode Mode;

    /// <summary>
    /// Whether the actor is on ground.
    /// </summary>
    public bool OnGround;

    /// <summary>
    /// Runtime id of the ridden entity.
    /// </summary>
    public ulong RiddenRuntimeId;

    /// <summary>
    /// Teleport cause when mode is teleport.
    /// </summary>
    public TeleportCause TeleportCause;

    /// <summary>
    /// Source entity type for teleports.
    /// </summary>
    public int TeleportSourceEntityType;

    /// <summary>
    /// Server tick for this move.
    /// </summary>
    public ulong Tick;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

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

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        Position.Write(writer);
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
