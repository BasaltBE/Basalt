#nullable enable

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
        RecipeNetId.Read(reader);
        FilteredStringIndex = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        RecipeNetId.Write(writer);
        writer.WriteInt32(FilteredStringIndex, true);
    }
}
