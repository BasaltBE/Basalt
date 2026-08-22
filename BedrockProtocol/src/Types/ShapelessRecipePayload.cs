using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ShapelessRecipePayload : DataType {
    public string RecipeId = string.Empty;
    public RecipeIngredient[] Ingredients = [];
    public NetworkItemInstanceDescriptor[] Results = [];
    public Uuid Uuid = new();
    public string Tag = string.Empty;
    public int Priority;
    public RecipeUnlockingRequirement? UnlockingRequirement;
    public uint NetId;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        ShapedRecipePayload.WriteIngredients(ref writer, Ingredients);
        ShapedRecipePayload.WriteResults(ref writer, Results);
        Uuid.Write(ref writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(UnlockingRequirement is not null);
        if (UnlockingRequirement is RecipeUnlockingRequirement requirement) requirement.Write(ref writer);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        Ingredients = ShapedRecipePayload.ReadIngredients(ref reader);
        Results = ShapedRecipePayload.ReadResults(ref reader);
        Uuid.Read(ref reader);
        Tag = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        UnlockingRequirement = reader.ReadBool() ? new RecipeUnlockingRequirement() : null;
        UnlockingRequirement?.Read(ref reader);
        NetId = reader.ReadVarUInt();
    }
}
