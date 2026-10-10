using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(2)]
public sealed class PlayStatusPacket : DataPacket {
    public int Status;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteInt32(Status, true);

    public override void Deserialize(ref BinaryReader reader) => Status = reader.ReadInt32(true);
}
