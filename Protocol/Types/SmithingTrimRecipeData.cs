using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SmithingTrimRecipeData : DataType
{
    public string RecipeId = string.Empty;
    public ItemDescriptorCount Template = new();
    public ItemDescriptorCount Base = new();
    public ItemDescriptorCount Addition = new();
    public string Block = "smithing_table";
    public uint RecipeNetworkId;

    public void Read(BinaryReader reader)
    {
        RecipeId = reader.ReadVarString();
        Template.Read(reader);
        Base.Read(reader);
        Addition.Read(reader);
        Block = reader.ReadVarString();
        RecipeNetworkId = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(RecipeId);
        Template.Write(writer);
        Base.Write(writer);
        Addition.Write(writer);
        writer.WriteVarString(Block);
        writer.WriteVarUInt(RecipeNetworkId);
    }
}
