using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class UseItemOnEntityInventoryTransactionData : IInventoryTransactionData
{
    public InventoryTransactionType Type => InventoryTransactionType.UseItemOnEntity;

    public ulong TargetEntityRuntimeId { get; set; }
    public uint ActionType { get; set; }
    public int HotBarSlot { get; set; }
    public ItemInstance HeldItem { get; set; } = new();
    public Vec3f Position { get; set; }
    public Vec3f ClickedPosition { get; set; }

    public void Read(ref BinaryReader reader)
    {
        TargetEntityRuntimeId = reader.ReadVarULong();
        ActionType = reader.ReadVarUInt();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(ref reader);
        Position.Read(ref reader);
        ClickedPosition.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarULong(TargetEntityRuntimeId);
        writer.WriteVarUInt(ActionType);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(ref writer);
        Position.Write(ref writer);
        ClickedPosition.Write(ref writer);
    }
}
