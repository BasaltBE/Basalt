#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftRecipeAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipe;
    public RecipeNetId RecipeNetId = new();
    public byte NumberOfRequestedCrafts;

    public void Read(BinaryReader reader) {
        RecipeNetId.Read(reader);
        NumberOfRequestedCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        RecipeNetId.Write(writer);
        writer.WriteUInt8(NumberOfRequestedCrafts);
    }
}
