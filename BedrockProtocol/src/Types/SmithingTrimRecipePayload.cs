using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SmithingTrimRecipePayload : DataType {
    public string RecipeId = string.Empty;
    public RecipeIngredient TemplateIngredient = new();
    public RecipeIngredient BaseIngredient = new();
    public RecipeIngredient AdditionIngredient = new();
    public string Tag = string.Empty;
    public uint NetId;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        TemplateIngredient.Write(ref writer);
        BaseIngredient.Write(ref writer);
        AdditionIngredient.Write(ref writer);
        writer.WriteVarString(Tag);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        TemplateIngredient.Read(ref reader);
        BaseIngredient.Read(ref reader);
        AdditionIngredient.Read(ref reader);
        Tag = reader.ReadVarString();
        NetId = reader.ReadVarUInt();
    }
}
