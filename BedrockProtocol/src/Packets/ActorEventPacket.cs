using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(27)]
public sealed class ActorEventPacket : DataPacket {
    public ulong ActorRuntimeId;
    public byte EventId;
    public int Data;
    public Vec3? FireAtPosition;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteUInt8(EventId);
        writer.WriteVarInt(Data);
        writer.WriteBool(FireAtPosition is not null);
        if (FireAtPosition is Vec3 position) position.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        EventId = reader.ReadUInt8();
        Data = reader.ReadVarInt();
        FireAtPosition = reader.ReadBool() ? new Vec3() : null;
        FireAtPosition?.Read(ref reader);
    }
}
