#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class InventoryOptions {
    public InventoryLeftTabIndex LeftInventoryTab;
    public InventoryRightTabIndex RightInventoryTab;
    public bool Filtering;
    public InventoryLayout LayoutInv;
    public InventoryLayout LayoutCraft;

    public void Read(BinaryReader reader) {
        LeftInventoryTab = (global::BedrockProtocol.Enums.InventoryLeftTabIndex)reader.ReadZigZag();
        RightInventoryTab = (global::BedrockProtocol.Enums.InventoryRightTabIndex)reader.ReadZigZag();
        Filtering = reader.ReadBool();
        LayoutInv = (global::BedrockProtocol.Enums.InventoryLayout)reader.ReadZigZag();
        LayoutCraft = (global::BedrockProtocol.Enums.InventoryLayout)reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)LeftInventoryTab);
        writer.WriteZigZag((int)RightInventoryTab);
        writer.WriteBool(Filtering);
        writer.WriteZigZag((int)LayoutInv);
        writer.WriteZigZag((int)LayoutCraft);
    }
}
