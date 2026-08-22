using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Enums;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemData : DataType {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public string ItemName = string.Empty;
    public short ItemId;
    public bool ComponentBased;
    public ItemVersion ItemVersion;
    public CompoundTag ComponentData = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(ItemName);
        writer.WriteInt16(ItemId, true);
        writer.WriteBool(ComponentBased);
        writer.WriteZigZag((int)ItemVersion);
        Nbt.WriteTag(writer, ComponentData, NetworkNbtOptions);
    }

    public override void Read(ref BinaryReader reader) {
        ItemName = reader.ReadVarString();
        ItemId = reader.ReadInt16(true);
        ComponentBased = reader.ReadBool();
        ItemVersion = (ItemVersion)reader.ReadZigZag();
        ComponentData = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }
}
