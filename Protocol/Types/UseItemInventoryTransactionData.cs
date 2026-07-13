using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemInventoryTransactionData : IInventoryTransactionData
{
    public InventoryTransactionType Type => InventoryTransactionType.UseItem;

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
    public NetworkItemStackDescriptor HeldItem = new();
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
    public byte ClientPrediction;
    /// <summary>
    /// Client cooldown state value.
    /// </summary>
    public byte ClientCooldownState;
    public void Read(BinaryReader reader)
    {
        ActionType = unchecked((uint)reader.ReadZigZag());
        TriggerType = reader.ReadUInt8();
        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(reader);
        BlockPosition = blockPosition;
        BlockFace = reader.ReadUInt8();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        Position.Read(reader);
        ClickedPosition.Read(reader);
        BlockRuntimeId = reader.ReadVarUInt();
        ClientPrediction = reader.ReadUInt8();
        ClientCooldownState = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(unchecked((int)ActionType));
        writer.WriteUInt8(unchecked((byte)TriggerType));
        BlockPosition.Write(writer);
        writer.WriteUInt8(unchecked((byte)BlockFace));
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        Position.Write(writer);
        ClickedPosition.Write(writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteUInt8(ClientPrediction);
        writer.WriteUInt8(ClientCooldownState);
    }
}
