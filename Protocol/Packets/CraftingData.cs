using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.CraftingData)]
public sealed record CraftingDataPacket : DataPacket
{
  public List<CraftingDataEntry> Recipes = [];
  public List<PotionRecipeData> PotionRecipes = [];
  public List<PotionContainerChangeRecipeData> PotionContainerChangeRecipes = [];
  public bool ClearRecipes = true;

  public override void Deserialize(Binary.BinaryReader reader)
  {
    int recipeCount = checked((int)reader.ReadVarUInt());
    Recipes = new List<CraftingDataEntry>(recipeCount);
    for (int i = 0; i < recipeCount; i++)
    {
      CraftingDataEntry entry = new();
      entry.Read(reader);
      Recipes.Add(entry);
    }

    int potionCount = checked((int)reader.ReadVarUInt());
    PotionRecipes = new List<PotionRecipeData>(potionCount);
    for (int i = 0; i < potionCount; i++)
    {
      PotionRecipeData potion = new();
      potion.Read(reader);
      PotionRecipes.Add(potion);
    }

    int containerChangeCount = checked((int)reader.ReadVarUInt());
    PotionContainerChangeRecipes = new List<PotionContainerChangeRecipeData>(containerChangeCount);
    for (int i = 0; i < containerChangeCount; i++)
    {
      PotionContainerChangeRecipeData change = new();
      change.Read(reader);
      PotionContainerChangeRecipes.Add(change);
    }

    int materialReducerCount = checked((int)reader.ReadVarUInt());
    for (int i = 0; i < materialReducerCount; i++)
    {
      reader.ReadZigZag();
      int outputCount = checked((int)reader.ReadVarUInt());
      for (int j = 0; j < outputCount; j++)
      {
        reader.ReadZigZag();
        reader.ReadZigZag();
      }
    }

    ClearRecipes = reader.ReadBool();
  }

  public override void Serialize(Binary.BinaryWriter writer)
  {
    writer.WriteVarUInt((uint)Recipes.Count);
    for (int i = 0; i < Recipes.Count; i++)
    {
      Recipes[i].Write(writer);
    }

    writer.WriteVarUInt((uint)PotionRecipes.Count);
    for (int i = 0; i < PotionRecipes.Count; i++)
    {
      PotionRecipes[i].Write(writer);
    }

    writer.WriteVarUInt((uint)PotionContainerChangeRecipes.Count);
    for (int i = 0; i < PotionContainerChangeRecipes.Count; i++)
    {
      PotionContainerChangeRecipes[i].Write(writer);
    }

    // Material reducers (empty).
    writer.WriteVarUInt(0);

    writer.WriteBool(ClearRecipes);
  }
}
