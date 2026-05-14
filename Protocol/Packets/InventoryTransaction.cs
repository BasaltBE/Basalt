using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventoryTransactionPacket : DataPacket
{
    public int LegacyRequestId { get; set; }
    public List<LegacySetItemSlot> LegacySetItemSlots { get; set; } = [];
    public IInventoryTransactionData TransactionData { get; set; } = new NormalInventoryTransactionData();
    public List<InventoryAction> Actions { get; set; } = [];

    public override PacketId PacketId => PacketId.InventoryTransaction;

    public override void Deserialize(ref BinaryReader reader)
    {
        LegacyRequestId = reader.ReadZigZag();
        LegacySetItemSlots = [];
        if (LegacyRequestId != 0)
        {
            int legacySetItemSlotCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new(legacySetItemSlotCount);
            for (int i = 0; i < legacySetItemSlotCount; i++)
            {
                LegacySetItemSlot legacySetItemSlot = new();
                legacySetItemSlot.Read(ref reader);
                LegacySetItemSlots.Add(legacySetItemSlot);
            }
        }

        InventoryTransactionType type = (InventoryTransactionType)reader.ReadVarUInt();
        IInventoryTransactionData transactionData = InventoryTransactionDataFactory.Create(type);

        int actionCount = checked((int)reader.ReadVarUInt());
        if (actionCount < 0 || actionCount > 4096)
        {
            throw new InvalidOperationException("Invalid action count.");
        }

        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(ref reader);
            Actions.Add(action);
        }

        transactionData.Read(ref reader);
        TransactionData = transactionData;
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteZigZag(LegacyRequestId);
        if (LegacyRequestId != 0)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(ref writer);
            }
        }

        writer.WriteVarUInt((uint)TransactionData.Type);

        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(ref writer);
        }

        TransactionData.Write(ref writer);
    }
}
