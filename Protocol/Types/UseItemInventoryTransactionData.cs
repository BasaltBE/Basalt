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

    public void Read(ref BinaryReader reader)
    {
        ActionType = reader.ReadVarUInt();
        TriggerType = reader.ReadVarUInt();
        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(ref reader);
        BlockPosition = blockPosition;
        BlockFace = reader.ReadZigZag();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(ref reader);
        Position.Read(ref reader);
        ClickedPosition.Read(ref reader);
        BlockRuntimeId = reader.ReadVarUInt();
        ClientPrediction = reader.ReadVarUInt();
        ClientCooldownState = reader.ReadUInt8();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(ActionType);
        writer.WriteVarUInt(TriggerType);
        BlockPosition.Write(ref writer);
        writer.WriteZigZag(BlockFace);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(ref writer);
        Position.Write(ref writer);
        ClickedPosition.Write(ref writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(ClientPrediction);
        writer.WriteUInt8(ClientCooldownState);
    }
}
