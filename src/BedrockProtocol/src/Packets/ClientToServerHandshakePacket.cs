namespace Basalt.BedrockProtocol.Packets;

[PacketId(4)]
public sealed class ClientToServerHandshakePacket : DataPacket {
    public override void Serialize(ref Basalt.Binary.BinaryWriter writer) {
    }

    public override void Deserialize(ref Basalt.Binary.BinaryReader reader) {
    }
}
