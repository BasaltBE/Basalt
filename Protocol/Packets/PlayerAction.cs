using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.PlayerAction)]
public sealed record PlayerActionPacket : DataPacket {
    /// <summary>
    /// Runtime id of the actor performing the action.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Action type requested by the client.
    /// </summary>
    public PlayerActionType ActionType;

    /// <summary>
    /// Primary block position for the action.
    /// </summary>
    public BlockPos BlockPosition;

    /// <summary>
    /// Secondary/result block position for the action.
    /// </summary>
    public BlockPos ResultPosition;

    /// <summary>
    /// Block face associated with the action.
    /// </summary>
    public int BlockFace;

    public override void Deserialize(Binary.BinaryReader reader) {
        EntityRuntimeId = reader.ReadVarULong();
        ActionType = (PlayerActionType)reader.ReadVarInt();

        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(reader);
        BlockPosition = blockPosition;

        BlockPos resultPosition = ResultPosition;
        resultPosition.Read(reader);
        ResultPosition = resultPosition;

        BlockFace = reader.ReadVarInt();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteVarInt((int)ActionType);
        BlockPosition.Write(writer);
        ResultPosition.Write(writer);
        writer.WriteVarInt(BlockFace);
    }
}
