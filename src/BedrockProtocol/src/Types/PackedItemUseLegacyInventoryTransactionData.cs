using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PackedItemUseLegacyInventoryTransactionData : DataType {
    public int LegacyRequestId;
    public LegacySetSlot[]? LegacySetItemSlots;
    public InventoryActionData[] Actions = [];
    public ItemUseInventoryTransactionData ItemUseTransaction = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt(LegacyRequestId);
        writer.WriteBool(LegacySetItemSlots is not null);
        if (LegacySetItemSlots is not null) {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Length);
            foreach (LegacySetSlot slot in LegacySetItemSlots) slot.Write(ref writer);
        }
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (InventoryActionData action in Actions) action.Write(ref writer);
        writer.WriteVarInt((int)ItemUseTransaction.ActionType);
        writer.WriteUInt8((byte)ItemUseTransaction.TriggerType);
        ItemUseTransaction.Position.Write(ref writer);
        writer.WriteUInt8(ItemUseTransaction.Face);
        writer.WriteVarInt(ItemUseTransaction.Slot);
        ItemUseTransaction.Item.Write(ref writer);
        ItemUseTransaction.FromPosition.Write(ref writer);
        ItemUseTransaction.ClickPosition.Write(ref writer);
        writer.WriteVarUInt(ItemUseTransaction.TargetBlockId);
        writer.WriteUInt8((byte)ItemUseTransaction.ClientInteractPrediction);
        writer.WriteUInt8((byte)ItemUseTransaction.ClientCooldownState);
    }

    public override void Read(ref BinaryReader reader) {
        LegacyRequestId = reader.ReadVarInt();
        LegacySetItemSlots = reader.ReadBool() ? new LegacySetSlot[checked((int)reader.ReadVarUInt())] : null;
        if (LegacySetItemSlots is not null) {
            for (int index = 0; index < LegacySetItemSlots.Length; index++) {
                LegacySetSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots[index] = slot;
            }
        }
        Actions = new InventoryActionData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Actions.Length; index++) {
            InventoryActionData action = new();
            action.Read(ref reader);
            Actions[index] = action;
        }
        ItemUseTransaction.ActionType = (ItemUseActionType)reader.ReadVarInt();
        ItemUseTransaction.TriggerType = (ItemUseTriggerType)reader.ReadUInt8();
        ItemUseTransaction.Position.Read(ref reader);
        ItemUseTransaction.Face = reader.ReadUInt8();
        ItemUseTransaction.Slot = reader.ReadVarInt();
        ItemUseTransaction.Item.Read(ref reader);
        ItemUseTransaction.FromPosition.Read(ref reader);
        ItemUseTransaction.ClickPosition.Read(ref reader);
        ItemUseTransaction.TargetBlockId = reader.ReadVarUInt();
        ItemUseTransaction.ClientInteractPrediction = (ItemUsePredictedResult)reader.ReadUInt8();
        ItemUseTransaction.ClientCooldownState = (ClientCooldownState)reader.ReadUInt8();
    }
}
