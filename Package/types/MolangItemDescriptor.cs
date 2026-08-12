#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MolangItemDescriptor : ItemStackRequestNetworkItemInstanceDescriptorItemDescriptorVariant, RecipeIngredientItemDescriptorVariant {
    public ItemDescriptorType DescriptorType = global::BedrockProtocol.Enums.ItemDescriptorType.Molang;
    public string TagExpression = string.Empty;
    public MolangVersion MolangVersion;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemDescriptorType constValue0 = (global::BedrockProtocol.Enums.ItemDescriptorType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemDescriptorType.Molang) {
            throw new FormatException($"Expected molang for DescriptorType, got {constValue0}.");
        }
        TagExpression = reader.ReadVarString();
        MolangVersion = (global::BedrockProtocol.Enums.MolangVersion)reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemDescriptorType.Molang);
        writer.WriteVarString(TagExpression);
        writer.WriteInt16((short)MolangVersion, true);
    }
}
