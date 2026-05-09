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
        int startOffset = reader.Offset;
        LegacyRequestId = reader.ReadZigZag();
        LegacySetItemSlots = [];
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            int legacyCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new List<LegacySetItemSlot>(legacyCount);
            for (int i = 0; i < legacyCount; i++)
            {
                LegacySetItemSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots.Add(slot);
            }
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new List<InventoryAction>(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(ref reader);
            Actions.Add(action);
        }

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

        int endOffset = reader.Offset;
        ReadOnlySpan<byte> payload = reader.Buffer.Slice(startOffset, endOffset - startOffset);
        Console.WriteLine($"[UseItemTxDump] bytes={payload.Length} hex={Convert.ToHexString(payload)} legacy={LegacyRequestId} actions={Actions.Count} action={ActionType} trigger={TriggerType} pos={BlockPosition.X},{BlockPosition.Y},{BlockPosition.Z} face={BlockFace} hotbar={HotBarSlot} runtime={BlockRuntimeId} prediction={ClientPrediction} cooldown={ClientCooldownState}");
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
