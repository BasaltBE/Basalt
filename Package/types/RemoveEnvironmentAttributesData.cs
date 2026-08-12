#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RemoveEnvironmentAttributesData : ClientboundAttributeLayerSyncDataVariant {
    public string AttributeLayerName = string.Empty;
    public DimensionType AttributeLayerDimension = new();
    public List<string> Attributes = [];

    public void Read(BinaryReader reader) {
        AttributeLayerName = reader.ReadVarString();
        AttributeLayerDimension.Read(reader);
        int count4 = checked((int)reader.ReadVarUInt());
        Attributes = new List<string>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string item4 = default!;
            item4 = reader.ReadVarString();
            Attributes.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(AttributeLayerName);
        AttributeLayerDimension.Write(writer);
        writer.WriteVarUInt(checked((uint)Attributes.Count));
        foreach (var item5 in Attributes) {
            writer.WriteVarString(item5);
        }
    }
}
