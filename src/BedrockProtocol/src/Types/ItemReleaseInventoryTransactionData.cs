using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemReleaseInventoryTransactionData : DataType {
    public ItemReleaseActionType ActionType;
    public int Slot;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 FromPosition = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt((int)ActionType);
        writer.WriteVarInt(Slot);
        Item.Write(ref writer);
        FromPosition.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        ActionType = (ItemReleaseActionType)reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Item.Read(ref reader);
        FromPosition.Read(ref reader);
    }
}
