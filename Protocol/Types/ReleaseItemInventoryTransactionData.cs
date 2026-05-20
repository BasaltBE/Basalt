using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ReleaseItemInventoryTransactionData : IInventoryTransactionData
{
    public InventoryTransactionType Type => InventoryTransactionType.ReleaseItem;

    public uint ActionType { get; set; }
    public int HotBarSlot { get; set; }
    public ItemInstance HeldItem { get; set; } = new();
    public Vec3f HeadPosition { get; set; }

    public void Read(BinaryReader reader)
    {
        ActionType = reader.ReadVarUInt();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        HeadPosition.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(ActionType);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        HeadPosition.Write(writer);
    }
}
