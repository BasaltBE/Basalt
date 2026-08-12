#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EmptyItemDescriptor : ItemStackRequestNetworkItemInstanceDescriptorItemDescriptorVariant, RecipeIngredientItemDescriptorVariant {
    public ItemDescriptorType DescriptorType = global::BedrockProtocol.Enums.ItemDescriptorType.Empty;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemDescriptorType constValue0 = (global::BedrockProtocol.Enums.ItemDescriptorType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemDescriptorType.Empty) {
            throw new FormatException($"Expected empty for DescriptorType, got {constValue0}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemDescriptorType.Empty);
    }
}
