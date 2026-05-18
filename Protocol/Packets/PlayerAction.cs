using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record PlayerActionPacket : DataPacket
{
    public ulong EntityRuntimeId { get; set; }
    public int ActionType { get; set; }
    public BlockPos BlockPosition { get; set; }
    public BlockPos ResultPosition { get; set; }
    public int BlockFace { get; set; }

    public override PacketId PacketId => PacketId.PlayerAction;

    public override void Deserialize(BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        ActionType = reader.ReadVarInt();
        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(reader);
        BlockPosition = blockPosition;
        BlockPos resultPosition = ResultPosition;
        resultPosition.Read(reader);
        ResultPosition = resultPosition;
        BlockFace = reader.ReadVarInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteVarInt(ActionType);
        BlockPosition.Write(writer);
        ResultPosition.Write(writer);
        writer.WriteVarInt(BlockFace);
    }
}
