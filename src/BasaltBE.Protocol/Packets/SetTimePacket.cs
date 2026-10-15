using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(10)]
public sealed class SetTimePacket : DataPacket {
    public int Time;

    public override void Deserialize(ref BinaryReader reader) => Time = reader.ReadVarInt();

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarInt(Time);
}
