using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftRecipeOptionalAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional;
    public RecipeNetId RecipeNetId = new();
    public int FilteredStringIndex;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional) {
            throw new FormatException($"Expected craftrecipeoptional for ActionType, got {constValue0}.");
        }
        RecipeNetId.Read(reader);
        FilteredStringIndex = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional);
        RecipeNetId.Write(writer);
        writer.WriteInt32(FilteredStringIndex, true);
    }
}
