using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

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
        WriteIngredients(ref writer, Ingredients);
        WriteResults(ref writer, Results);
        Uuid.Write(ref writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(AssumeSymmetry);
        writer.WriteBool(UnlockingRequirement is not null);
        if (UnlockingRequirement is RecipeUnlockingRequirement requirement) requirement.Write(ref writer);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        Width = reader.ReadZigZag();
        Height = reader.ReadZigZag();
        Ingredients = ReadIngredients(ref reader);
        Results = ReadResults(ref reader);
        Uuid.Read(ref reader);
        Tag = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        AssumeSymmetry = reader.ReadBool();
        UnlockingRequirement = reader.ReadBool() ? new RecipeUnlockingRequirement() : null;
        UnlockingRequirement?.Read(ref reader);
        NetId = reader.ReadVarUInt();
    }

    internal static void WriteIngredients(ref BinaryWriter writer, RecipeIngredient[] values) {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    internal static RecipeIngredient[] ReadIngredients(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        RecipeIngredient[] values = new RecipeIngredient[count];
        for (int i = 0; i < count; i++) {
            values[i] = new RecipeIngredient();
            values[i].Read(ref reader);
        }
        return values;
    }

    internal static void WriteResults(ref BinaryWriter writer, NetworkItemInstanceDescriptor[] values) {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    internal static NetworkItemInstanceDescriptor[] ReadResults(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        NetworkItemInstanceDescriptor[] values = new NetworkItemInstanceDescriptor[count];
        for (int i = 0; i < count; i++) {
            values[i] = new NetworkItemInstanceDescriptor();
            values[i].Read(ref reader);
        }
        return values;
    }
}
