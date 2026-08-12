#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemTagDescriptor : ItemStackRequestNetworkItemInstanceDescriptorItemDescriptorVariant, RecipeIngredientItemDescriptorVariant {
    public ItemDescriptorType DescriptorType = global::BedrockProtocol.Enums.ItemDescriptorType.ItemTag;
    public string ItemTag = string.Empty;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemDescriptorType constValue0 = (global::BedrockProtocol.Enums.ItemDescriptorType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemDescriptorType.ItemTag) {
            throw new FormatException($"Expected itemtag for DescriptorType, got {constValue0}.");
        }
        ItemTag = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemDescriptorType.ItemTag);
        writer.WriteVarString(ItemTag);
    }
}
