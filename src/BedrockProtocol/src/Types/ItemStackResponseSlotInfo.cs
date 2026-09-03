using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemStackResponseSlotInfo : DataType {
    public byte Slot;
    public byte HotbarSlot;
    public byte Count;
    public int? ItemStackId;
    public string CustomName = string.Empty;
    public string FilteredCustomName = string.Empty;
    public int DurabilityCorrection;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(HotbarSlot);
        writer.WriteUInt8(Count);
        writer.WriteBool(ItemStackId.HasValue);
        if (ItemStackId is int itemStackId) writer.WriteVarInt(itemStackId);
        writer.WriteVarString(CustomName);
        writer.WriteVarString(FilteredCustomName);
        writer.WriteVarInt(DurabilityCorrection);
    }

    public override void Read(ref BinaryReader reader) {
        Slot = reader.ReadUInt8();
        HotbarSlot = reader.ReadUInt8();
        Count = reader.ReadUInt8();
        ItemStackId = reader.ReadBool() ? reader.ReadVarInt() : null;
        CustomName = reader.ReadVarString();
        FilteredCustomName = reader.ReadVarString();
        DurabilityCorrection = reader.ReadVarInt();
    }
}
