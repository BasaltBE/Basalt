using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackResponseSlotInfo : DataType
{
    /// <summary>
    /// Slot index in the container.
    /// </summary>
    public byte Slot;
    /// <summary>
    /// Hotbar slot mirror value.
    /// </summary>
    public byte HotbarSlot;
    /// <summary>
    /// Item count for the slot.
    /// </summary>
    public byte Count;
    /// <summary>
    /// Stack network id for the slot.
    /// </summary>
    public int StackNetworkId;
    /// <summary>
    /// Optional custom item name.
    /// </summary>
    public string CustomName = string.Empty;
    /// <summary>
    /// Filtered custom item name.
    /// </summary>
    public string FilteredCustomName = string.Empty;
    /// <summary>
    /// Durability correction value.
    /// </summary>
    public int DurabilityCorrection;
    public void Read(BinaryReader reader)
    {
        byte requestedSlot = reader.ReadUInt8();
        byte slot = reader.ReadUInt8();
        Slot = (byte)(requestedSlot & slot);
        HotbarSlot = Slot;
        Count = reader.ReadUInt8();
        StackNetworkId = reader.ReadZigZag();
        CustomName = reader.ReadVarString();
        FilteredCustomName = reader.ReadVarString();
        DurabilityCorrection = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(Count);
        writer.WriteZigZag(StackNetworkId);
        writer.WriteVarString(CustomName);
        writer.WriteVarString(FilteredCustomName);
        writer.WriteZigZag(DurabilityCorrection);
    }
}
