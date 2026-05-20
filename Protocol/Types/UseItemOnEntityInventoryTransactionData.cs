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
