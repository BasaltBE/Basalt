using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(17)]
public sealed class TakeItemActorPacket : DataPacket {
    public ulong ItemRuntimeId;
    public ulong ActorRuntimeId;

    public override void Deserialize(ref BinaryReader reader) {
        ItemRuntimeId = reader.ReadVarULong();
        ActorRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ItemRuntimeId);
        writer.WriteVarULong(ActorRuntimeId);
    }
}
