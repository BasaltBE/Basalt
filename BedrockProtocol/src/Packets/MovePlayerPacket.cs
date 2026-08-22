using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(19)]
public sealed class MovePlayerPacket : DataPacket {
    public ulong ActorRuntimeId;
    public Vec3 Position = new();
    public Vec2 Rotation = new();
    public float HeadRotation;
    public byte PositionMode;
    public bool OnGround;
    public ulong RidingRuntimeId;
    public MovePlayerTeleportData? TeleportData;
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        Position.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        writer.WriteUInt8(PositionMode);
        writer.WriteBool(OnGround);
        writer.WriteVarULong(RidingRuntimeId);
        writer.WriteBool(TeleportData is not null);
        if (TeleportData is MovePlayerTeleportData teleportData) teleportData.Write(ref writer);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        Position.Read(ref reader);
        Rotation.Read(ref reader);
        HeadRotation = reader.ReadF32(true);
        PositionMode = reader.ReadUInt8();
        OnGround = reader.ReadBool();
        RidingRuntimeId = reader.ReadVarULong();
        TeleportData = reader.ReadBool() ? new MovePlayerTeleportData() : null;
        TeleportData?.Read(ref reader);
        Tick = reader.ReadVarULong();
    }
}
