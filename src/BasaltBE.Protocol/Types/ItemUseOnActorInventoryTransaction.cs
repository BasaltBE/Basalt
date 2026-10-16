using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemUseOnActorInventoryTransaction : InventoryTransactionData {
    public override uint TypeId => 3;
    public InventoryTransaction Actions = new();
    public ulong RuntimeId;
    public ItemUseOnActorInventoryTransactionActionType ActionType;
    public int Slot;
    public HandSlot Hand;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 HitPosition = new();

    public override void Read(ref BinaryReader reader) {
        Actions.Read(ref reader);
        RuntimeId = reader.ReadVarULong();
        ActionType = (ItemUseOnActorInventoryTransactionActionType)reader.ReadVarInt();
        Slot = reader.ReadZigZag();
        Hand = (HandSlot)reader.ReadUInt8();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
        HitPosition.Read(ref reader);
    }

    public override void Write(ref BinaryWriter writer) {
        Actions.Write(ref writer);
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarInt((int)ActionType);
        writer.WriteZigZag(Slot);
        writer.WriteUInt8((byte)Hand);
        Item.Write(ref writer);
        FromPosition.Write(ref writer);
        HitPosition.Write(ref writer);
    }
}
