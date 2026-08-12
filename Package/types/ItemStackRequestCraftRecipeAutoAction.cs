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
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto) {
            throw new FormatException($"Expected craftrecipeauto for ActionType, got {constValue0}.");
        }
        RecipeNetId.Read(reader);
        NumberOfRequestedCrafts = reader.ReadUInt8();
        int count6 = checked((int)reader.ReadVarUInt());
        Ingredients = new List<RecipeIngredient>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            RecipeIngredient item6 = default!;
            RecipeIngredient readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            Ingredients.Add(item6);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto);
        RecipeNetId.Write(writer);
        writer.WriteUInt8(NumberOfRequestedCrafts);
        writer.WriteVarUInt(checked((uint)Ingredients.Count));
        foreach (var item7 in Ingredients) {
            item7.Write(writer);
        }
    }
}
