using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ShapedRecipePayload : DataType {
    public string RecipeId = string.Empty;
    public int Width;
    public int Height;
    public RecipeIngredient[] Ingredients = [];
    public NetworkItemInstanceDescriptor[] Results = [];
    public Uuid Uuid = new();
    public string Tag = string.Empty;
    public int Priority;
    public bool AssumeSymmetry;
    public RecipeUnlockingRequirement? UnlockingRequirement;
    public uint NetId;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        writer.WriteZigZag(Width);
        writer.WriteZigZag(Height);
        writer.WriteVarUInt((uint)Ingredients.Length);
        for (int i = 0; i < Ingredients.Length; i++)
            Ingredients[i].Write(ref writer);
        writer.WriteVarUInt((uint)Results.Length);
        for (int i = 0; i < Results.Length; i++)
            Results[i].Write(ref writer);
        Uuid.Write(ref writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(AssumeSymmetry);
        writer.WriteBool(UnlockingRequirement is not null);
        UnlockingRequirement?.Write(ref writer);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        Width = reader.ReadZigZag();
        Height = reader.ReadZigZag();
        Ingredients = new RecipeIngredient[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Ingredients.Length; i++) {
            Ingredients[i] = new RecipeIngredient();
            Ingredients[i].Read(ref reader);
        }
        Results = new NetworkItemInstanceDescriptor[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Results.Length; i++) {
            Results[i] = new NetworkItemInstanceDescriptor();
            Results[i].Read(ref reader);
        }
        Uuid.Read(ref reader);
        Tag = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        AssumeSymmetry = reader.ReadBool();
        UnlockingRequirement = reader.ReadBool() ? new RecipeUnlockingRequirement() : null;
        UnlockingRequirement?.Read(ref reader);
        NetId = reader.ReadVarUInt();
    }

}
