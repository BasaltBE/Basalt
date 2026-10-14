using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemUseInventoryTransaction : InventoryTransactionData {
    public override uint TypeId => 2;
    public InventoryTransaction Actions = new();
    public ItemUseInventoryTransactionActionType ActionType;
    public ItemUseInventoryTransactionTriggerType TriggerType;
    public BlockPos Position = new();
    public byte Face;
    public int Slot;
    public HandSlot Hand;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 ClickPosition = new();
    public uint TargetBlockId;
    public ItemUseInventoryTransactionPredictedResult ClientInteractPrediction;
    public ItemUseInventoryTransactionClientCooldownState ClientCooldownState;

    public override void Read(ref BinaryReader reader) {
        Actions.Read(ref reader);
        ActionType = (ItemUseInventoryTransactionActionType)reader.ReadVarInt();
        TriggerType = (ItemUseInventoryTransactionTriggerType)reader.ReadUInt8();
        Position.Read(ref reader);
        Face = reader.ReadUInt8();
        Slot = reader.ReadVarInt();
        Hand = (HandSlot)reader.ReadUInt8();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
        ClickPosition.Read(ref reader);
        TargetBlockId = reader.ReadVarUInt();
        ClientInteractPrediction = (ItemUseInventoryTransactionPredictedResult)reader.ReadUInt8();
        ClientCooldownState = (ItemUseInventoryTransactionClientCooldownState)reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        Actions.Write(ref writer);
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
}
