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
        int bodyStart = reader.Offset;
        int[] legacyModes =
        [
            LegacyRequestId != 0 ? 1 : 0,
            LegacyRequestId < -1 && (unchecked((uint)LegacyRequestId) & 1) == 0 ? 1 : 0,
            0
        ];

        Exception? lastError = null;
        for (int i = 0; i < legacyModes.Length; i++)
        {
            reader.Seek(bodyStart);
            try
            {
                DeserializeBody(ref reader, parseLegacySlots: legacyModes[i] == 1);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("Failed to parse InventoryTransaction.");
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

    private void DeserializeBody(ref BinaryReader reader, bool parseLegacySlots)
    {
        int bodyStart = reader.Offset;
        if (parseLegacySlots)
        {
            int legacyCount = checked((int)reader.ReadVarUInt());
            if (legacyCount < 0 || legacyCount > 512)
            {
                throw new InvalidOperationException("Invalid legacy slot count.");
            }

            LegacySetItemSlots = new(legacyCount);
            for (int i = 0; i < legacyCount; i++)
            {
                LegacySetItemSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots.Add(slot);
            }
        }
        else
        {
            LegacySetItemSlots = [];
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
}
