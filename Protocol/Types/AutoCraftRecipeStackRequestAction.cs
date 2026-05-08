using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class AutoCraftRecipeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 13;
    public uint RecipeNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public byte TimesCrafted { get; set; }
    public List<ItemDescriptorCount> Ingredients { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        TimesCrafted = reader.ReadUInt8();
        int ingredientCount = checked((int)reader.ReadVarUInt());
        Ingredients = new(ingredientCount);
        for (int i = 0; i < ingredientCount; i++)
        {
            ItemDescriptorCount ingredient = new();
            ingredient.Read(ref reader);
            Ingredients.Add(ingredient);
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteUInt8(TimesCrafted);
        writer.WriteVarUInt((uint)Ingredients.Count);
        for (int i = 0; i < Ingredients.Count; i++)
        {
            Ingredients[i].Write(ref writer);
        }
    }
}
