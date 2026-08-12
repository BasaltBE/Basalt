#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UpdateEnvironmentAttributesData : ClientboundAttributeLayerSyncDataVariant {
    public string AttributeLayerName = string.Empty;
    public DimensionType AttributeLayerDimension = new();
    public List<EnvironmentAttributeData> Attributes = [];

    public void Read(BinaryReader reader) {
        AttributeLayerName = reader.ReadVarString();
        AttributeLayerDimension.Read(reader);
        int count4 = checked((int)reader.ReadVarUInt());
        Attributes = new List<EnvironmentAttributeData>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            EnvironmentAttributeData item4 = default!;
            EnvironmentAttributeData readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            Attributes.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(AttributeLayerName);
        AttributeLayerDimension.Write(writer);
        writer.WriteVarUInt(checked((uint)Attributes.Count));
        foreach (var item5 in Attributes) {
            item5.Write(writer);
        }
    }
}
