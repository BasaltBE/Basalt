using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class InventoryAction : DataType
{
    public uint SourceType { get; set; }
    public int WindowId { get; set; }
    public uint SourceFlags { get; set; }
    public uint InventorySlot { get; set; }
    public ItemInstance OldItem { get; set; } = new();
    public ItemInstance NewItem { get; set; } = new();

    public void Read(BinaryReader reader)
    {
        SourceType = reader.ReadVarUInt();
        if (SourceType == 0 || SourceType == 99999)
        {
            WindowId = reader.ReadZigZag();
        }
        else if (SourceType == 2)
        {
            SourceFlags = reader.ReadVarUInt();
        }

        InventorySlot = reader.ReadVarUInt();
        OldItem.Read(reader);
        NewItem.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(SourceType);
        if (SourceType == 0 || SourceType == 99999)
        {
            writer.WriteZigZag(WindowId);
        }
        else if (SourceType == 2)
        {
            writer.WriteVarUInt(SourceFlags);
        }

        writer.WriteVarUInt(InventorySlot);
        OldItem.Write(writer);
        NewItem.Write(writer);
    }
}
