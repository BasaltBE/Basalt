using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ClientboundDataDrivenUIClose)]
public sealed record ClientboundDataDrivenUIClosePacket : DataPacket
{
    public uint? FormId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        FormId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteBool(FormId.HasValue);
        if (FormId.HasValue)
        {
            writer.WriteUInt32(FormId.Value, true);
        }
    }
}
