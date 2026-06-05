using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemTransactionData : DataType
{
    /// <summary>
    /// Legacy request id from older inventory flow.
    /// </summary>
    public int LegacyRequestId;
    /// <summary>
    /// Legacy slot updates tied to the legacy request.
    /// </summary>
    public List<LegacySetItemSlot> LegacySetItemSlots = [];
    /// <summary>
    /// Inventory actions included in this transaction.
    /// </summary>
    public List<InventoryAction> Actions = [];
    /// <summary>
    /// Use-item action type.
    /// </summary>
    public uint ActionType;
    /// <summary>
    /// Trigger source for this transaction.
    /// </summary>
    public uint TriggerType;
    /// <summary>
    /// Target block position.
    /// </summary>
    public BlockPos BlockPosition;
    /// <summary>
    /// Block face used for the action.
    /// </summary>
    public int BlockFace;
    /// <summary>
    /// Hotbar slot used by the client.
    /// </summary>
    public int HotBarSlot;
    /// <summary>
    /// Item held by the player.
    /// </summary>
    public ItemInstance HeldItem = new();
    /// <summary>
    /// Player position at action time.
    /// </summary>
    public Vec3f Position;
    /// <summary>
    /// Clicked position relative to target.
    /// </summary>
    public Vec3f ClickedPosition;
    /// <summary>
    /// Block runtime id seen by the client.
    /// </summary>
    public uint BlockRuntimeId;
    /// <summary>
    /// Client-side prediction state.
    /// </summary>
    public uint ClientPrediction;
    /// <summary>
    /// Client cooldown state value.
    /// </summary>
    public byte ClientCooldownState;
    public void Read(BinaryReader reader)
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
                slot.Read(reader);
                LegacySetItemSlots.Add(slot);
            }
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new List<InventoryAction>(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(reader);
            Actions.Add(action);
        }

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

        int endOffset = reader.Offset;
        ReadOnlySpan<byte> payload = reader.Buffer.Slice(startOffset, endOffset - startOffset);
        Console.WriteLine($"[UseItemTxDump] bytes={payload.Length} hex={Convert.ToHexString(payload)} legacy={LegacyRequestId} actions={Actions.Count} action={ActionType} trigger={TriggerType} pos={BlockPosition.X},{BlockPosition.Y},{BlockPosition.Z} face={BlockFace} hotbar={HotBarSlot} runtime={BlockRuntimeId} prediction={ClientPrediction} cooldown={ClientCooldownState}");
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(LegacyRequestId);
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(writer);
            }
        }

        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(writer);
        }

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
