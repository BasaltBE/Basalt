using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(35)]
public sealed class ActorPickRequestPacket : DataPacket {
    public long ActorId;
    public byte MaxSlots;
    public bool WithData;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt64(ActorId, true);
        writer.WriteUInt8(MaxSlots);
        writer.WriteBool(WithData);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorId = reader.ReadInt64(true);
        MaxSlots = reader.ReadUInt8();
        WithData = reader.ReadBool();
    }
}
