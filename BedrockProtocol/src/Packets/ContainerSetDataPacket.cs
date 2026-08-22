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
        writer.WriteUInt8((byte)ContainerId);
        writer.WriteVarInt(Property);
        writer.WriteVarInt(Value);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadUInt8();
        Property = reader.ReadVarInt();
        Value = reader.ReadVarInt();
    }
}
