using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

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
        writer.WriteVarUInt((uint)Ingredients.Length);
        for (int i = 0; i < Ingredients.Length; i++)
            Ingredients[i].Write(ref writer);
        writer.WriteVarUInt((uint)Results.Length);
        for (int i = 0; i < Results.Length; i++)
            Results[i].Write(ref writer);
        Uuid.Write(ref writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(UnlockingRequirement is not null);
        UnlockingRequirement?.Write(ref writer);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        RecipeId = reader.ReadVarString();
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
        UnlockingRequirement = reader.ReadBool() ? new RecipeUnlockingRequirement() : null;
        UnlockingRequirement?.Read(ref reader);
        NetId = reader.ReadVarUInt();
    }
}
