using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftRecipeAutoActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftRecipeAuto;
    public int RecipeNetId;
    public byte NumberOfRequestedCrafts;
    public RecipeIngredientData[] Ingredients = [];
    public override void Read(ref BinaryReader reader) {
        RecipeNetId = checked((int)reader.ReadVarUInt());
        NumberOfRequestedCrafts = reader.ReadUInt8();
        uint count = reader.ReadVarUInt();
        Ingredients = new RecipeIngredientData[(int)count];
        for (int i = 0; i < Ingredients.Length; i++) { Ingredients[i] = new RecipeIngredientData(); Ingredients[i].Read(ref reader); }
    }
    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)RecipeNetId));
        writer.WriteUInt8(NumberOfRequestedCrafts);
        writer.WriteVarUInt((uint)Ingredients.Length);
        foreach (RecipeIngredientData ingredient in Ingredients) ingredient.Write(ref writer);
    }
}
