using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ContainerClosePacket : DataPacket
{
    public byte WindowId { get; set; }
    public byte ContainerType { get; set; }
    public bool ServerSide { get; set; }

    public override PacketId PacketId => PacketId.ContainerClose;

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ServerSide = reader.ReadBool();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8(WindowId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerSide);
    }
}
