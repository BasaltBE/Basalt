using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class AutoCraftRecipeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 13;
    /// <summary>
    /// Network id of the recipe to craft.
    /// </summary>
    public uint RecipeNetworkId;
    /// <summary>
    /// Requested craft count value.
    /// </summary>
    public byte NumberOfCrafts;
    /// <summary>
    /// Times the recipe was crafted.
    /// </summary>
    public byte TimesCrafted;
    /// <summary>
    /// Ingredient descriptors used by the request.
    /// </summary>
    public List<ItemDescriptorCount> Ingredients = [];

    public void Read(BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        TimesCrafted = reader.ReadUInt8();
        int ingredientCount = checked((int)reader.ReadVarUInt());
        Ingredients = new(ingredientCount);
        for (int i = 0; i < ingredientCount; i++)
        {
            ItemDescriptorCount ingredient = new();
            ingredient.Read(reader);
            Ingredients.Add(ingredient);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteUInt8(TimesCrafted);
        writer.WriteVarUInt((uint)Ingredients.Count);
        for (int i = 0; i < Ingredients.Count; i++)
        {
            Ingredients[i].Write(writer);
        }
    }
}
