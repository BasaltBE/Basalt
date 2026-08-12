#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ShapelessRecipePayload {
    public string RecipeId = string.Empty;
    public List<RecipeIngredient_1164730002> Ingredients = [];
    public List<NetworkItemInstanceDescriptorData> Results = [];
    public UUID UUID = new();
    public string Tag = string.Empty;
    public int Priority;
    public RecipeUnlockingRequirement? UnlockingRequirement;
    public RecipeNetId NetId = new();

    public void Read(BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        int count2 = checked((int)reader.ReadVarUInt());
        Ingredients = new List<RecipeIngredient_1164730002>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            RecipeIngredient_1164730002 item2 = default!;
            RecipeIngredient_1164730002 readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            Ingredients.Add(item2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        Results = new List<NetworkItemInstanceDescriptorData>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            NetworkItemInstanceDescriptorData item4 = default!;
            NetworkItemInstanceDescriptorData readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            Results.Add(item4);
        }
        UUID.Read(reader);
        Tag = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        if (reader.ReadBool()) {
            RecipeUnlockingRequirement readValue12 = new();
            readValue12.Read(reader);
            UnlockingRequirement = readValue12;
        } else {
            UnlockingRequirement = default;
        }
        NetId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        writer.WriteVarUInt(checked((uint)Ingredients.Count));
        foreach (var item3 in Ingredients) {
            item3.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)Results.Count));
        foreach (var item5 in Results) {
            item5.Write(writer);
        }
        UUID.Write(writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(UnlockingRequirement is not null);
        if (UnlockingRequirement is { } optionalValue13) {
            optionalValue13.Write(writer);
        }
        NetId.Write(writer);
    }
}
