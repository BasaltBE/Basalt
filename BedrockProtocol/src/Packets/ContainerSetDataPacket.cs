using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(51)]
public sealed class ContainerSetDataPacket : DataPacket {
    public ContainerId ContainerId;
    public int Property;
    public int Value;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt8((sbyte)ContainerId);
        writer.WriteZigZag(Property);
        writer.WriteZigZag(Value);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadInt8();
        Property = reader.ReadZigZag();
        Value = reader.ReadZigZag();
    }
}
