using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class InventoryAction : DataType
{
    /// <summary>
    /// Source type of this action.
    /// </summary>
    public uint SourceType;
    /// <summary>
    /// Window id for container sources.
    /// </summary>
    public int WindowId;
    /// <summary>
    /// Source flags for world sources.
    /// </summary>
    public uint SourceFlags;
    /// <summary>
    /// Slot index affected by the action.
    /// </summary>
    public uint InventorySlot;
    /// <summary>
    /// Item state before the action.
    /// </summary>
    public ItemInstance OldItem = new();
    /// <summary>
    /// Item state after the action.
    /// </summary>
    public ItemInstance NewItem = new();

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
