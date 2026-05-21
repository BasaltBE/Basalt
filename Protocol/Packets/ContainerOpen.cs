using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ContainerOpen)]
public sealed record ContainerOpenPacket : DataPacket
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
    /// Container block position.
    /// </summary>
    public BlockPos ContainerPosition;

    /// <summary>
    /// Unique id of the container entity.
    /// </summary>
    public long ContainerEntityUniqueId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        WindowId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();

        BlockPos containerPosition = ContainerPosition;
        containerPosition.Read(reader);
        ContainerPosition = containerPosition;

        ContainerEntityUniqueId = reader.ReadZigZong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteUInt8(WindowId);
        writer.WriteUInt8(ContainerType);
        ContainerPosition.Write(writer);
        writer.WriteZigZong(ContainerEntityUniqueId);
    }
}
