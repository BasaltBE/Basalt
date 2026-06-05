using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ContainerClose)]
public sealed record ContainerClosePacket : DataPacket
{
    /// <summary>
    /// Window id of the container.
    /// </summary>
    public byte WindowId;

    /// <summary>
    /// Container type id.
    /// </summary>
    public byte ContainerType;

    /// <summary>
    /// Whether this close is server initiated.
    /// </summary>
    public bool ServerSide;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        WindowId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ServerSide = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteUInt8(WindowId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerSide);
    }
}
