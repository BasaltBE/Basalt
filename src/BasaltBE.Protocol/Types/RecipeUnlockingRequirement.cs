using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class RecipeUnlockingRequirement : DataType {
    public int UnlockingContext;
    public RecipeIngredient[]? UnlockingIngredients;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(UnlockingContext);
        writer.WriteBool(UnlockingIngredients is not null);
        if (UnlockingIngredients is null)
            return;
        writer.WriteVarUInt((uint)UnlockingIngredients.Length);
        foreach (RecipeIngredient ingredient in UnlockingIngredients)
            ingredient.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        UnlockingContext = reader.ReadZigZag();
        if (!reader.ReadBool()) {
            UnlockingIngredients = null;
            return;
        }
        int count = checked((int)reader.ReadVarUInt());
        UnlockingIngredients = new RecipeIngredient[count];
        for (int i = 0; i < count; i++) {
            UnlockingIngredients[i] = new RecipeIngredient();
            UnlockingIngredients[i].Read(ref reader);
        }
    }
}
