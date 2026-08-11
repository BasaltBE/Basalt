using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemUseOnActorInventoryTransaction : InventoryTransactionVariant {
    public InventoryTransaction Actions = new();
    public ActorRuntimeID RuntimeId = new();
    public ItemUseOnActorActionType ActionType;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 HitPosition = new();

    public void Read(BinaryReader reader) {
        RuntimeId.Read(reader);
        ActionType = (global::BedrockProtocol.Enums.ItemUseOnActorActionType)reader.ReadZigZag();
        Slot = reader.ReadZigZag();
        Item.Read(reader);
        FromPosition.Read(reader);
        HitPosition.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        RuntimeId.Write(writer);
        writer.WriteZigZag((int)ActionType);
        writer.WriteZigZag(Slot);
        Item.Write(writer);
        FromPosition.Write(writer);
        HitPosition.Write(writer);
    }
}
