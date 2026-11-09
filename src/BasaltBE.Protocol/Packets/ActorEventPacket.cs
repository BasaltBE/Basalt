using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(27)]
public sealed class ActorEventPacket : DataPacket {
    public ulong TargetRuntimeId;
    public ActorEvent Event;
    public int Data;
    public Vec3? FireAtPosition;

    public override void Deserialize(ref BinaryReader reader) {
        TargetRuntimeId = reader.ReadVarULong();
        Event = (ActorEvent)reader.ReadUInt8();
        Data = reader.ReadVarInt();
        FireAtPosition = reader.ReadBool() ? new Vec3() : null;
        FireAtPosition?.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(TargetRuntimeId);
        writer.WriteUInt8((byte)Event);
        writer.WriteVarInt(Data);
        writer.WriteBool(FireAtPosition is not null);
        FireAtPosition?.Write(ref writer);
    }
}
