using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ContainerClose)]
public sealed record ContainerClosePacket : DataPacket
{
    /// <summary>
    /// Container id of the window being closed.
    /// </summary>
    public ContainerId ContainerId;

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
        ContainerId = (ContainerId)reader.ReadInt8();
        ContainerType = reader.ReadUInt8();
        ServerSide = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteInt8((sbyte)ContainerId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerSide);
    }
}
