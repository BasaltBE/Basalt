using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(30)]
public sealed class InventoryTransactionPacket : DataPacket {
    public int LegacyRequestId;
    public LegacySetSlot[]? LegacySetItemSlots;
    public InventoryTransactionData Transaction = new NormalTransactionData();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt(LegacyRequestId);
        writer.WriteBool(LegacySetItemSlots is not null);
        if (LegacySetItemSlots is not null) {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Length);
            foreach (LegacySetSlot slot in LegacySetItemSlots) slot.Write(ref writer);
        }
        Transaction.WriteTransaction(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        LegacyRequestId = reader.ReadVarInt();
        if (reader.ReadBool()) {
            uint count = reader.ReadVarUInt();
            if (count > 100 || count > reader.Remaining)
                throw new InvalidDataException("Inventory transaction contains too many legacy slot sets.");
            LegacySetItemSlots = new LegacySetSlot[(int)count];
            for (int i = 0; i < LegacySetItemSlots.Length; i++) {
                LegacySetItemSlots[i] = new LegacySetSlot();
                LegacySetItemSlots[i].Read(ref reader);
            }
        }
        Transaction = InventoryTransactionData.ReadTransaction(ref reader, reader.ReadVarUInt());
    }
}
