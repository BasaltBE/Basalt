#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemData {
    public string ItemName = string.Empty;
    public short ItemId;
    public bool IsComponentBased;
    public ItemVersion ItemVersion;
    public object ItemComponentData = null!;

    public delegate object ItemComponentDataReader(BinaryReader reader);
    public delegate void ItemComponentDataWriter(BinaryWriter writer, object value);

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("ItemData requires external reader callbacks for: ItemComponentData. Use the Read overload that accepts them.");
    }

    public void Read(BinaryReader reader, ItemComponentDataReader readItemComponentData) {
        ItemName = reader.ReadVarString();
        ItemId = reader.ReadInt16(true);
        IsComponentBased = reader.ReadBool();
        ItemVersion = (global::BedrockProtocol.Enums.ItemVersion)reader.ReadZigZag();
        ItemComponentData = readItemComponentData(reader);
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("ItemData requires external writer callbacks for: ItemComponentData. Use the Write overload that accepts them.");
    }

    public void Write(BinaryWriter writer, ItemComponentDataWriter writeItemComponentData) {
        writer.WriteVarString(ItemName);
        writer.WriteInt16(ItemId, true);
        writer.WriteBool(IsComponentBased);
        writer.WriteZigZag((int)ItemVersion);
        writeItemComponentData(writer, ItemComponentData);
    }
}
