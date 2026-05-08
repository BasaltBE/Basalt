using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ContainerOpenPacket : DataPacket
{
    public byte WindowId { get; set; }
    public byte ContainerType { get; set; }
    public BlockPos ContainerPosition { get; set; }
    public long ContainerEntityUniqueId { get; set; }

    public override PacketId PacketId => PacketId.ContainerOpen;

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ContainerPosition.Read(ref reader);
        ContainerEntityUniqueId = reader.ReadZigZong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8(WindowId);
        writer.WriteUInt8(ContainerType);
        ContainerPosition.Write(ref writer);
        writer.WriteZigZong(ContainerEntityUniqueId);
    }
}
