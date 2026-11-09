using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(47)]
public sealed class ContainerClosePacket : DataPacket {
    public byte ContainerId;
    public byte ContainerType;
    public bool ServerInitiatedClose;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8(ContainerId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerInitiatedClose);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ServerInitiatedClose = reader.ReadBool();
    }
}
