using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SmithingTransformRecipePayload {
    public string RecipeId = string.Empty;
    public RecipeIngredient_1164730002 TemplateIngredient = new();
    public RecipeIngredient_1164730002 BaseIngredient = new();
    public RecipeIngredient_1164730002 AdditionIngredient = new();
    public NetworkItemInstanceDescriptorData Result = new();
    public string Tag = string.Empty;
    public RecipeNetId NetId = new();

    public void Read(BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        TemplateIngredient.Read(reader);
        BaseIngredient.Read(reader);
        AdditionIngredient.Read(reader);
        Result.Read(reader);
        Tag = reader.ReadVarString();
        NetId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        TemplateIngredient.Write(writer);
        BaseIngredient.Write(writer);
        AdditionIngredient.Write(writer);
        Result.Write(writer);
        writer.WriteVarString(Tag);
        NetId.Write(writer);
    }
}
