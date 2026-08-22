using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemUseOnActorInventoryTransactionData : DataType {
    public ulong RuntimeId;
    public ItemUseOnActorActionType ActionType;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();
    public Vec3 HitPosition = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarInt((int)ActionType);
        writer.WriteVarInt(Slot);
        Item.Write(ref writer);
        FromPosition.Write(ref writer);
        HitPosition.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        RuntimeId = reader.ReadVarULong();
        ActionType = (ItemUseOnActorActionType)reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
        HitPosition.Read(ref reader);
    }
}
