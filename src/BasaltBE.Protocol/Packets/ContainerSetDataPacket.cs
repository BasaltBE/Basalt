using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(51)]
public sealed class ContainerSetDataPacket : DataPacket {
    public byte ContainerId;
    public int Property;
    public int Value;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt8(unchecked((sbyte)ContainerId));
        writer.WriteZigZag(Property);
        writer.WriteZigZag(Value);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = unchecked((byte)reader.ReadInt8());
        Property = reader.ReadZigZag();
        Value = reader.ReadZigZag();
    }
}
