#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UpdateAttributeLayersData : ClientboundAttributeLayerSyncDataVariant {
    public List<AttributeLayerData> AttributeLayers = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        AttributeLayers = new List<AttributeLayerData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            AttributeLayerData item0 = default!;
            AttributeLayerData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            AttributeLayers.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)AttributeLayers.Count));
        foreach (var item1 in AttributeLayers) {
            item1.Write(writer);
        }
    }
}
