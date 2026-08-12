#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UpdateAttributeLayerSettingsData : ClientboundAttributeLayerSyncDataVariant {
    public string AttributeLayerName = string.Empty;
    public DimensionType AttributeLayerDimension = new();
    public AttributeLayerSettings AttributesLayerSettings = new();

    public void Read(BinaryReader reader) {
        AttributeLayerName = reader.ReadVarString();
        AttributeLayerDimension.Read(reader);
        AttributesLayerSettings.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(AttributeLayerName);
        AttributeLayerDimension.Write(writer);
        AttributesLayerSettings.Write(writer);
    }
}
