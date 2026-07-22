using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ClientToServerHandshake)]
public sealed record ClientToServerHandshakePacket : DataPacket {
    public override void Serialize(Binary.BinaryWriter writer) {
    }

    public override void Deserialize(Binary.BinaryReader reader) {
    }
}
