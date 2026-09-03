using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemUseInventoryTransactionData : DataType {
    public ItemUseActionType ActionType;
    public ItemUseTriggerType TriggerType;
    public BlockPos Position = new();
    public byte Face;
    public int Slot;
    public HandSlot Hand;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 ClickPosition = new();
    public uint TargetBlockId;
    public ItemUsePredictedResult ClientInteractPrediction;
    public ClientCooldownState ClientCooldownState;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt((int)ActionType);
        writer.WriteUInt8((byte)TriggerType);
        Position.Write(ref writer);
        writer.WriteUInt8(Face);
        writer.WriteVarInt(Slot);
        writer.WriteUInt8((byte)Hand);
        Item.Write(ref writer);
        FromPosition.Write(ref writer);
        ClickPosition.Write(ref writer);
        writer.WriteVarUInt(TargetBlockId);
        writer.WriteUInt8((byte)ClientInteractPrediction);
        writer.WriteUInt8((byte)ClientCooldownState);
    }

    public override void Read(ref BinaryReader reader) {
        ActionType = (ItemUseActionType)reader.ReadVarInt();
        TriggerType = (ItemUseTriggerType)reader.ReadUInt8();
        Position.Read(ref reader);
        Face = reader.ReadUInt8();
        Slot = reader.ReadVarInt();
        Hand = (HandSlot)reader.ReadUInt8();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
        ClickPosition.Read(ref reader);
        TargetBlockId = reader.ReadVarUInt();
        ClientInteractPrediction = (ItemUsePredictedResult)reader.ReadUInt8();
        ClientCooldownState = (ClientCooldownState)reader.ReadUInt8();
    }
}
