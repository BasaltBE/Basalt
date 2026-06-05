using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ModalFormRequest)]
public sealed record ModalFormRequestPacket : DataPacket
{
    public int FormId;
    public string Payload = string.Empty;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        FormId = reader.ReadVarInt();
        Payload = reader.ReadVarString();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarInt(FormId);
        writer.WriteVarString(Payload);
    }
}
