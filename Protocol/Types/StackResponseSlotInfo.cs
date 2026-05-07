using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackResponseSlotInfo : DataType
{
    public byte Slot { get; set; }
    public byte HotbarSlot { get; set; }
    public byte Count { get; set; }
    public int StackNetworkId { get; set; }
    public string CustomName { get; set; } = string.Empty;
    public string FilteredCustomName { get; set; } = string.Empty;
    public int DurabilityCorrection { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Slot = reader.ReadUInt8();
        HotbarSlot = reader.ReadUInt8();
        Count = reader.ReadUInt8();
        StackNetworkId = reader.ReadZigZag();
        CustomName = reader.ReadVarString();
        FilteredCustomName = reader.ReadVarString();
        DurabilityCorrection = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(HotbarSlot);
        writer.WriteUInt8(Count);
        writer.WriteZigZag(StackNetworkId);
        writer.WriteVarString(CustomName);
        writer.WriteVarString(FilteredCustomName);
        writer.WriteZigZag(DurabilityCorrection);
    }
}
