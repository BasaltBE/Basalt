using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ShapedRecipePayload {
    public string RecipeId = string.Empty;
    public int Width;
    public int Height;
    public List<RecipeIngredient_1164730002> Ingredients = [];
    public List<NetworkItemInstanceDescriptorData> Results = [];
    public UUID UUID = new();
    public string Tag = string.Empty;
    public int Priority;
    public bool AssumeSymmetry;
    public RecipeUnlockingRequirement? UnlockingRequirement;
    public RecipeNetId NetId = new();

    public void Read(BinaryReader reader) {
        RecipeId = reader.ReadVarString();
        Width = reader.ReadZigZag();
        Height = reader.ReadZigZag();
        int count6 = checked((int)reader.ReadVarUInt());
        Ingredients = new List<RecipeIngredient_1164730002>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            RecipeIngredient_1164730002 item6 = default!;
            RecipeIngredient_1164730002 readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            Ingredients.Add(item6);
        }
        int count8 = checked((int)reader.ReadVarUInt());
        Results = new List<NetworkItemInstanceDescriptorData>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            NetworkItemInstanceDescriptorData item8 = default!;
            NetworkItemInstanceDescriptorData readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            Results.Add(item8);
        }
        UUID.Read(reader);
        Tag = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        AssumeSymmetry = reader.ReadBool();
        if (reader.ReadBool()) {
            RecipeUnlockingRequirement readValue18 = new();
            readValue18.Read(reader);
            UnlockingRequirement = readValue18;
        } else {
            UnlockingRequirement = default;
        }
        NetId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(RecipeId);
        writer.WriteZigZag(Width);
        writer.WriteZigZag(Height);
        writer.WriteVarUInt(checked((uint)Ingredients.Count));
        foreach (var item7 in Ingredients) {
            item7.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)Results.Count));
        foreach (var item9 in Results) {
            item9.Write(writer);
        }
        UUID.Write(writer);
        writer.WriteVarString(Tag);
        writer.WriteZigZag(Priority);
        writer.WriteBool(AssumeSymmetry);
        writer.WriteBool(UnlockingRequirement is not null);
        if (UnlockingRequirement is { } optionalValue19) {
            optionalValue19.Write(writer);
        }
        NetId.Write(writer);
    }
}
