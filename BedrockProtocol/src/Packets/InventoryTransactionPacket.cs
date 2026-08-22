using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(30)]
public sealed class InventoryTransactionPacket : DataPacket {
    public int LegacyRequestId;
    public LegacySetSlot[]? LegacySetItemSlots;
    public InventoryActionData[] Actions = [];
    public InventoryTransactionType TransactionType;
    public NormalTransactionData NormalTransaction = new();
    public InventoryMismatchData InventoryMismatch = new();
    public ItemUseInventoryTransactionData ItemUseTransaction = new();
    public ItemUseOnActorInventoryTransactionData ItemUseOnActorTransaction = new();
    public ItemReleaseInventoryTransactionData ItemReleaseTransaction = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt(LegacyRequestId);
        writer.WriteBool(LegacySetItemSlots is not null);
        if (LegacySetItemSlots is not null) {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Length);
            foreach (LegacySetSlot slot in LegacySetItemSlots) slot.Write(ref writer);
        }
        writer.WriteVarUInt((uint)TransactionType);
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (InventoryActionData action in Actions) action.Write(ref writer);
        switch (TransactionType) {
            case InventoryTransactionType.Normal:
                NormalTransaction.Write(ref writer);
                break;
            case InventoryTransactionType.InventoryMismatch:
                InventoryMismatch.Write(ref writer);
                break;
            case InventoryTransactionType.ItemUse:
                ItemUseTransaction.Write(ref writer);
                break;
            case InventoryTransactionType.ItemUseOnActor:
                ItemUseOnActorTransaction.Write(ref writer);
                break;
            case InventoryTransactionType.ItemRelease:
                ItemReleaseTransaction.Write(ref writer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(TransactionType));
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        LegacyRequestId = reader.ReadVarInt();
        LegacySetItemSlots = reader.ReadBool() ? new LegacySetSlot[checked((int)reader.ReadVarUInt())] : null;
        if (LegacySetItemSlots is not null) {
            for (int index = 0; index < LegacySetItemSlots.Length; index++) {
                LegacySetSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots[index] = slot;
            }
        }
        TransactionType = (InventoryTransactionType)reader.ReadVarUInt();
        Actions = new InventoryActionData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Actions.Length; index++) {
            InventoryActionData action = new();
            action.Read(ref reader);
            Actions[index] = action;
        }
        switch (TransactionType) {
            case InventoryTransactionType.Normal:
                NormalTransaction.Read(ref reader);
                break;
            case InventoryTransactionType.InventoryMismatch:
                InventoryMismatch.Read(ref reader);
                break;
            case InventoryTransactionType.ItemUse:
                ItemUseTransaction.Read(ref reader);
                break;
            case InventoryTransactionType.ItemUseOnActor:
                ItemUseOnActorTransaction.Read(ref reader);
                break;
            case InventoryTransactionType.ItemRelease:
                ItemReleaseTransaction.Read(ref reader);
                break;
            default:
                throw new FormatException("Unsupported inventory transaction type.");
        }
    }
}
