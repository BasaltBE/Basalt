using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(14)]
public sealed class RemoveActorPacket : DataPacket {
    public long ActorUniqueId;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarLong(ActorUniqueId);
    public override void Deserialize(ref BinaryReader reader) => ActorUniqueId = reader.ReadVarLong();
}
