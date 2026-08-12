#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AttributeData {
    public float MinValue;
    public float MaxValue;
    public float CurrentValue;
    public float DefaultMinValue;
    public float DefaultMaxValue;
    public float DefaultValue;
    public string Name = string.Empty;
    public List<AttributeModifier> Modifiers = [];

    public void Read(BinaryReader reader) {
        MinValue = reader.ReadF32(true);
        MaxValue = reader.ReadF32(true);
        CurrentValue = reader.ReadF32(true);
        DefaultMinValue = reader.ReadF32(true);
        DefaultMaxValue = reader.ReadF32(true);
        DefaultValue = reader.ReadF32(true);
        Name = reader.ReadVarString();
        int count14 = checked((int)reader.ReadVarUInt());
        Modifiers = new List<AttributeModifier>(count14);
        for (int i14 = 0; i14 < count14; i14++) {
            AttributeModifier item14 = default!;
            AttributeModifier readValue1014 = new();
            readValue1014.Read(reader);
            item14 = readValue1014;
            Modifiers.Add(item14);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(MinValue, true);
        writer.WriteF32(MaxValue, true);
        writer.WriteF32(CurrentValue, true);
        writer.WriteF32(DefaultMinValue, true);
        writer.WriteF32(DefaultMaxValue, true);
        writer.WriteF32(DefaultValue, true);
        writer.WriteVarString(Name);
        writer.WriteVarUInt(checked((uint)Modifiers.Count));
        foreach (var item15 in Modifiers) {
            item15.Write(writer);
        }
    }
}
