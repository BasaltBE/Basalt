using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(113)]
public sealed class SetLocalPlayerAsInitializedPacket : DataPacket {
    public ulong RuntimeId;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarULong(RuntimeId);

    public override void Deserialize(ref BinaryReader reader) => RuntimeId = reader.ReadVarULong();
}
