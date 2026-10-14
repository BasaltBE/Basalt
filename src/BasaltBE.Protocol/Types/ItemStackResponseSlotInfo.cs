using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemStackResponseSlotInfo : DataType {
    public byte RequestedSlot;
    public byte Slot;
    public byte Amount;
    public int? ItemStackNetId;
    public RedactableString CustomName = new();
    public int DurabilityCorrection;

    public override void Read(ref BinaryReader reader) {
        RequestedSlot = reader.ReadUInt8();
        Slot = reader.ReadUInt8();
        Amount = reader.ReadUInt8();
        ItemStackNetId = reader.ReadBool() ? reader.ReadVarInt() : null;
        CustomName.Read(ref reader);
        DurabilityCorrection = reader.ReadVarInt();
    }

    public override void Write(ref BinaryWriter writer) {
        if (DurabilityCorrection is < short.MinValue or > short.MaxValue) throw new InvalidDataException("Item stack durability correction must fit in a signed 16-bit value.");
        writer.WriteUInt8(RequestedSlot);
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(Amount);
        writer.WriteBool(ItemStackNetId.HasValue);
        if (ItemStackNetId is int itemStackNetId) writer.WriteVarInt(itemStackNetId);
        CustomName.Write(ref writer);
        writer.WriteVarInt(DurabilityCorrection);
    }
}
