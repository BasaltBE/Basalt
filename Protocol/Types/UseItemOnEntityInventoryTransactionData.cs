using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemOnEntityInventoryTransactionData : IInventoryTransactionData
{
    public InventoryTransactionType Type => InventoryTransactionType.UseItemOnEntity;

    /// <summary>
    /// Runtime id of the target entity.
    /// </summary>
    public ulong TargetEntityRuntimeId;
    /// <summary>
    /// Use-on-entity action type.
    /// </summary>
    public uint ActionType;
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
    /// Clicked position relative to entity.
    /// </summary>
    public Vec3f ClickedPosition;
    public void Read(BinaryReader reader)
    {
        TargetEntityRuntimeId = reader.ReadVarULong();
        ActionType = reader.ReadVarUInt();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        Position.Read(reader);
        ClickedPosition.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarULong(TargetEntityRuntimeId);
        writer.WriteVarUInt(ActionType);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        Position.Write(writer);
        ClickedPosition.Write(writer);
    }
}
