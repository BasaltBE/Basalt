using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemUseInventoryTransaction : InventoryTransactionVariant {
    public InventoryTransaction Actions = new();
    public ItemUseActionType ActionType;
    public ItemUseTriggerType TriggerType;
    public BlockPos Position = new();
    public byte Face;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 ClickPosition = new();
    public uint TargetBlockId;
    public ItemUsePredictedResult ClientInteractPrediction;
    public ItemUseClientCooldownState ClientCooldownState;

    public void Read(BinaryReader reader) {
        ActionType = (global::BedrockProtocol.Enums.ItemUseActionType)reader.ReadZigZag();
        TriggerType = (global::BedrockProtocol.Enums.ItemUseTriggerType)reader.ReadUInt8();
        Position.Read(reader);
        Face = reader.ReadUInt8();
        Slot = reader.ReadZigZag();
        Item.Read(reader);
        FromPosition.Read(reader);
        ClickPosition.Read(reader);
        TargetBlockId = reader.ReadVarUInt();
        ClientInteractPrediction = (global::BedrockProtocol.Enums.ItemUsePredictedResult)reader.ReadUInt8();
        ClientCooldownState = (global::BedrockProtocol.Enums.ItemUseClientCooldownState)reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)ActionType);
        writer.WriteUInt8((byte)TriggerType);
        Position.Write(writer);
        writer.WriteUInt8(Face);
        writer.WriteZigZag(Slot);
        Item.Write(writer);
        FromPosition.Write(writer);
        ClickPosition.Write(writer);
        writer.WriteVarUInt(TargetBlockId);
        writer.WriteUInt8((byte)ClientInteractPrediction);
        writer.WriteUInt8((byte)ClientCooldownState);
    }
}
