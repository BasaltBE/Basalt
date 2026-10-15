using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(14)]
public sealed class RemoveActorPacket : DataPacket {
    public long ActorId;

    public override void Deserialize(ref BinaryReader reader) => ActorId = reader.ReadZigZong();

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZong(ActorId);
}
