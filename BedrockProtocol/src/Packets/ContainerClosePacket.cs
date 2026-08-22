using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(47)]
public sealed class ContainerClosePacket : DataPacket {
    public ContainerId ContainerId;
    public byte ContainerType;
    public bool ServerInitiatedClose;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerInitiatedClose);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ServerInitiatedClose = reader.ReadBool();
    }
}
