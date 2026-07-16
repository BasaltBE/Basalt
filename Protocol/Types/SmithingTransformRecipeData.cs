using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SmithingTransformRecipeData : DataType
{
  public string RecipeId = string.Empty;
  public ItemDescriptorCount Template = new();
  public ItemDescriptorCount Base = new();
  public ItemDescriptorCount Addition = new();
  public RecipeItemStack Result = new();
  public string Block = "smithing_table";
  public uint RecipeNetworkId;

  public void Read(BinaryReader reader)
  {
    RecipeId = reader.ReadVarString();
    Template.Read(reader);
    Base.Read(reader);
    Addition.Read(reader);
    Result.Read(reader);
    Block = reader.ReadVarString();
    RecipeNetworkId = reader.ReadVarUInt();
  }

  public void Write(BinaryWriter writer)
  {
    writer.WriteVarString(RecipeId);
    Template.Write(writer);
    Base.Write(writer);
    Addition.Write(writer);
    Result.Write(writer);
    writer.WriteVarString(Block);
    writer.WriteVarUInt(RecipeNetworkId);
  }
}
