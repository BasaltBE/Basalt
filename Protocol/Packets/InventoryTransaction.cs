using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.InventoryTransaction)]
public sealed record InventoryTransactionPacket : DataPacket
{
    /// <summary>
    /// Legacy request id.
    /// </summary>
    public int LegacyRequestId;

    /// <summary>
    /// Legacy set-slot entries.
    /// </summary>
    public List<LegacySetItemSlot> LegacySetItemSlots = [];

    /// <summary>
    /// Typed transaction data payload.
    /// </summary>
    public IInventoryTransactionData TransactionData = new NormalInventoryTransactionData();

    /// <summary>
    /// Inventory action list.
    /// </summary>
    public List<InventoryAction> Actions = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        LegacyRequestId = reader.ReadZigZag();
        LegacySetItemSlots = [];
        bool hasLegacy = reader.ReadBool();
        if (hasLegacy)
        {
            int legacySetItemSlotCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new(legacySetItemSlotCount);
            for (int i = 0; i < legacySetItemSlotCount; i++)
            {
                LegacySetItemSlot legacySetItemSlot = new();
                legacySetItemSlot.Read(reader);
                LegacySetItemSlots.Add(legacySetItemSlot);
            }
        }

        if (!reader.ReadBool())
        {
            throw new FormatException("Inventory transaction type is missing.");
        }

        InventoryTransactionType type = (InventoryTransactionType)reader.ReadVarUInt();
        IInventoryTransactionData transactionData = InventoryTransactionDataFactory.Create(type);

        if (!reader.ReadBool())
        {
            throw new FormatException("Inventory transaction actions are missing.");
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        if (actionCount < 0 || actionCount > 4096)
        {
            throw new InvalidOperationException("Invalid action count.");
        }

        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(reader);
            Actions.Add(action);
        }

        transactionData.Read(reader);
        TransactionData = transactionData;
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteZigZag(LegacyRequestId);
        bool hasLegacy = LegacyRequestId < -1 && (LegacyRequestId & 1) == 0;
        writer.WriteBool(hasLegacy);
        if (hasLegacy)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(writer);
            }
        }

        writer.WriteBool(true);
        writer.WriteVarUInt((uint)TransactionData.Type);

        writer.WriteBool(true);
        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(writer);
        }

        TransactionData.Write(writer);
    }
}
