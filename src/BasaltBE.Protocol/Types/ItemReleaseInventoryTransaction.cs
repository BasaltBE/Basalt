using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemReleaseInventoryTransaction : InventoryTransactionData {
    public override uint TypeId => 4;
    public InventoryTransaction Actions = new();
    public ItemReleaseInventoryTransactionActionType ActionType;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public HandSlot Hand;

    public override void Read(ref BinaryReader reader) {
        Actions.Read(ref reader);
        ActionType = (ItemReleaseInventoryTransactionActionType)reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
        Hand = (HandSlot)reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        Actions.Write(ref writer);
        writer.WriteVarInt((int)ActionType);
        writer.WriteVarInt(Slot);
        Item.Write(ref writer);
        FromPosition.Write(ref writer);
        writer.WriteUInt8((byte)Hand);
    }
}
