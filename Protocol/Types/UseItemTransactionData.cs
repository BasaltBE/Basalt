using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemTransactionData : DataType
{
    public int LegacyRequestId { get; set; }
    public List<LegacySetItemSlot> LegacySetItemSlots { get; set; } = [];
    public List<InventoryAction> Actions { get; set; } = [];
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
        LegacyRequestId = reader.ReadZigZag();
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            int legacyCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new(legacyCount);
            for (int i = 0; i < legacyCount; i++)
            {
                LegacySetItemSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots.Add(slot);
            }
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(ref reader);
            Actions.Add(action);
        }

        ActionType = reader.ReadVarUInt();
        TriggerType = reader.ReadVarUInt();
        BlockPosition.Read(ref reader);
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
        writer.WriteZigZag(LegacyRequestId);
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(ref writer);
            }
        }

        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(ref writer);
        }

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
