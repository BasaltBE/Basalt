namespace Basalt.BedrockProtocol.Packets;

[PacketId(310)]
public sealed class ClientboundCloseFormPacket : DataPacket {
    public override void Serialize(ref Basalt.Binary.BinaryWriter writer) {
    }

    public override void Deserialize(ref Basalt.Binary.BinaryReader reader) {
    }
}
