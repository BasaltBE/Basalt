using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RecipeUnlockingRequirement {
    public RecipeUnlockingContext UnlockingContext;
    public List<RecipeIngredient_1164730002>? UnlockingIngredients;

    public void Read(BinaryReader reader) {
        UnlockingContext = (global::BedrockProtocol.Enums.RecipeUnlockingContext)reader.ReadZigZag();
        if (reader.ReadBool()) {
            int count2 = checked((int)reader.ReadVarUInt());
            UnlockingIngredients = new List<RecipeIngredient_1164730002>(count2);
            for (int i2 = 0; i2 < count2; i2++) {
                RecipeIngredient_1164730002 item2 = default!;
                RecipeIngredient_1164730002 readValue1002 = new();
                readValue1002.Read(reader);
                item2 = readValue1002;
                UnlockingIngredients.Add(item2);
            }
        } else {
            UnlockingIngredients = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)UnlockingContext);
        writer.WriteBool(UnlockingIngredients is not null);
        if (UnlockingIngredients is { } optionalValue3) {
            writer.WriteVarUInt(checked((uint)optionalValue3.Count));
            foreach (var item3 in optionalValue3) {
                item3.Write(writer);
            }
        }
    }
}
