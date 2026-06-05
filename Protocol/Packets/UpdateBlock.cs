using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

/// <summary>
/// @Direction Clientbound
/// Sent by the server to update a block client side.
/// </summary>s
[Packet(PacketId.UpdateBlock)]
public sealed record UpdateBlockPacket : DataPacket
{
    /// <summary>
    /// World position of the block being updated.
    /// </summary>
    public BlockPos Position;

    /// <summary>
    /// Runtime network block state id.
    /// </summary>
    public uint NetworkBlockId;

    /// <summary>
    /// Update behavior flags for neighbors/network/permutation.
    /// </summary>
    public UpdateBlockFlagsType Flags;

    /// <summary>
    /// Target block layer.
    /// </summary>
    public UpdateBlockLayerType Layer;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        NetworkBlockId = reader.ReadVarUInt();
        Flags = (UpdateBlockFlagsType)reader.ReadVarUInt();
        Layer = (UpdateBlockLayerType)reader.ReadVarUInt();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        Position.Write(writer);
        writer.WriteVarUInt(NetworkBlockId);
        writer.WriteVarUInt((uint)Flags);
        writer.WriteVarUInt((uint)Layer);
    }
}
