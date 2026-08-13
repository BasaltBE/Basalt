#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftRecipeAutoAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto;
    public RecipeNetId RecipeNetId = new();
    public byte NumberOfRequestedCrafts;
    public List<RecipeIngredient> Ingredients = [];

    public void Read(BinaryReader reader) {
        RecipeNetId.Read(reader);
        NumberOfRequestedCrafts = reader.ReadUInt8();
        int count4 = checked((int)reader.ReadVarUInt());
        Ingredients = new List<RecipeIngredient>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            RecipeIngredient item4 = default!;
            RecipeIngredient readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            Ingredients.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        RecipeNetId.Write(writer);
        writer.WriteUInt8(NumberOfRequestedCrafts);
        writer.WriteVarUInt(checked((uint)Ingredients.Count));
        foreach (var item5 in Ingredients) {
            item5.Write(writer);
        }
    }
}
