#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AttributeLayerData {
    public string Name = string.Empty;
    public string? NoiseName;
    public DimensionType Dimension = new();
    public AttributeLayerSettings Settings = new();
    public List<EnvironmentAttributeData> Attributes = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        if (reader.ReadBool()) {
            NoiseName = reader.ReadVarString();
        } else {
            NoiseName = default;
        }
        Dimension.Read(reader);
        Settings.Read(reader);
        int count8 = checked((int)reader.ReadVarUInt());
        Attributes = new List<EnvironmentAttributeData>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            EnvironmentAttributeData item8 = default!;
            EnvironmentAttributeData readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            Attributes.Add(item8);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(NoiseName is not null);
        if (NoiseName is { } optionalValue3) {
            writer.WriteVarString(optionalValue3);
        }
        Dimension.Write(writer);
        Settings.Write(writer);
        writer.WriteVarUInt(checked((uint)Attributes.Count));
        foreach (var item9 in Attributes) {
            item9.Write(writer);
        }
    }
}
