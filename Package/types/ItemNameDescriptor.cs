using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemNameDescriptor : ItemStackRequestNetworkItemInstanceDescriptorItemDescriptorVariant, RecipeIngredientItemDescriptorVariant {
    public ItemDescriptorType DescriptorType = global::BedrockProtocol.Enums.ItemDescriptorType.ItemName;
    public string FullName = string.Empty;
    public int AuxValue;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemDescriptorType constValue0 = (global::BedrockProtocol.Enums.ItemDescriptorType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemDescriptorType.ItemName) {
            throw new FormatException($"Expected itemname for DescriptorType, got {constValue0}.");
        }
        FullName = reader.ReadVarString();
        AuxValue = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemDescriptorType.ItemName);
        writer.WriteVarString(FullName);
        writer.WriteZigZag(AuxValue);
    }
}
