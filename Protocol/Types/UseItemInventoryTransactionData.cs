using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemInventoryTransactionData : IInventoryTransactionData
{
    public InventoryTransactionType Type => InventoryTransactionType.UseItem;

    public uint ActionType { get; set; }
    public uint TriggerType { get; set; }
    public BlockPos BlockPosition { get; set; }
    public int BlockFace { get; set; }
    public int HotBarSlot { get; set; }
    public ItemInstance HeldItem { get; set; } = new();
    public Vec3f Position { get; set; }
    public Vec3f ClickedPosition { get; set; }
    public uint BlockRuntimeId { get; set; }
    public uint ClientPrediction { get; set; }
    public byte ClientCooldownState { get; set; }

    public void Read(BinaryReader reader)
    {
        ActionType = reader.ReadVarUInt();
        TriggerType = reader.ReadVarUInt();
        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(reader);
        BlockPosition = blockPosition;
        BlockFace = reader.ReadZigZag();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        Position.Read(reader);
        ClickedPosition.Read(reader);
        BlockRuntimeId = reader.ReadVarUInt();
        ClientPrediction = reader.ReadVarUInt();
        ClientCooldownState = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(ActionType);
        writer.WriteVarUInt(TriggerType);
        BlockPosition.Write(writer);
        writer.WriteZigZag(BlockFace);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        Position.Write(writer);
        ClickedPosition.Write(writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(ClientPrediction);
        writer.WriteUInt8(ClientCooldownState);
    }
}
