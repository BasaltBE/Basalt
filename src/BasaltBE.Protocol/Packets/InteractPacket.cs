using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(33)]
public sealed class InteractPacket : DataPacket {
    public InteractAction Action;
    public ulong TargetRuntimeId;
    public Vec3? Position;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        writer.WriteVarULong(TargetRuntimeId);
        writer.WriteBool(Position is not null);
        Position?.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Action = (InteractAction)reader.ReadUInt8();
        TargetRuntimeId = reader.ReadVarULong();
        Position = reader.ReadBool() ? new Vec3() : null;
        Position?.Read(ref reader);
    }
}
