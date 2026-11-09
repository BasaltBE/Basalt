using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PackedItemUseLegacyInventoryTransaction : DataType {
    public int LegacyRequestId;
    public LegacySetSlot[]? LegacySetItemSlots;
    public ItemUseInventoryTransaction ItemUseTransaction = new();

    public override void Read(ref BinaryReader reader) {
        LegacyRequestId = reader.ReadVarInt();
        if (reader.ReadBool()) {
            uint count = reader.ReadVarUInt();
            if (count > 100 || count > reader.Remaining)
                throw new InvalidDataException("Item interaction contains too many legacy slot sets.");
            LegacySetItemSlots = new LegacySetSlot[(int)count];
            for (int i = 0; i < LegacySetItemSlots.Length; i++) {
                LegacySetItemSlots[i] = new LegacySetSlot();
                LegacySetItemSlots[i].Read(ref reader);
            }
        }
        ItemUseTransaction.Read(ref reader);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt(LegacyRequestId);
        writer.WriteBool(LegacySetItemSlots is not null);
        if (LegacySetItemSlots is not null) {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Length);
            foreach (LegacySetSlot slot in LegacySetItemSlots) slot.Write(ref writer);
        }
        ItemUseTransaction.Write(ref writer);
    }
}
