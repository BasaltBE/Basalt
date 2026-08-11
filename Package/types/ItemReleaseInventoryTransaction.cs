using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemReleaseInventoryTransaction : InventoryTransactionVariant {
    public InventoryTransaction Actions = new();
    public ItemReleaseActionType ActionType;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();

    public void Read(BinaryReader reader) {
        ActionType = (global::BedrockProtocol.Enums.ItemReleaseActionType)reader.ReadZigZag();
        Slot = reader.ReadZigZag();
        Item.Read(reader);
        FromPosition.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)ActionType);
        writer.WriteZigZag(Slot);
        Item.Write(writer);
        FromPosition.Write(writer);
    }
}
