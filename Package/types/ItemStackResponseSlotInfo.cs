using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackResponseSlotInfo {
    public byte RequestedSlot;
    public byte Slot;
    public byte Amount;
    public ItemStackNetId? ItemStackNetId;
    public RedactableString CustomName = new();
    public int DurabilityCorrection;

    public void Read(BinaryReader reader) {
        RequestedSlot = reader.ReadUInt8();
        Slot = reader.ReadUInt8();
        Amount = reader.ReadUInt8();
        if (reader.ReadBool()) {
            ItemStackNetId readValue6 = new();
            readValue6.Read(reader);
            ItemStackNetId = readValue6;
        } else {
            ItemStackNetId = default;
        }
        CustomName.Read(reader);
        DurabilityCorrection = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(RequestedSlot);
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(Amount);
        writer.WriteBool(ItemStackNetId is not null);
        if (ItemStackNetId is { } optionalValue7) {
            optionalValue7.Write(writer);
        }
        CustomName.Write(writer);
        writer.WriteZigZag(DurabilityCorrection);
    }
}
