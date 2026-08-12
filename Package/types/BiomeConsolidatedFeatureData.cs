#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeConsolidatedFeatureData {
    public BiomeScatterParamData Scatter = new();
    public ushort Feature;
    public ushort Identifier;
    public ushort Pass;
    public bool CanUseInternalFeature;

    public void Read(BinaryReader reader) {
        Scatter.Read(reader);
        Feature = reader.ReadUInt16(true);
        Identifier = reader.ReadUInt16(true);
        Pass = reader.ReadUInt16(true);
        CanUseInternalFeature = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        Scatter.Write(writer);
        writer.WriteUInt16(Feature, true);
        writer.WriteUInt16(Identifier, true);
        writer.WriteUInt16(Pass, true);
        writer.WriteBool(CanUseInternalFeature);
    }
}
