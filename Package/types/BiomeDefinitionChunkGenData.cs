#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeDefinitionChunkGenData {
    public BiomeClimateData Climate = new();
    public BiomeConsolidatedFeaturesData ConsolidatedFeatures = new();
    public BiomeMountainParamsData MountainParams = new();
    public BiomeSurfaceMaterialAdjustmentData SurfaceMaterialAdjustments = new();
    public BiomeOverworldGenRulesData OverworldGenRules = new();
    public BiomeMultinoiseGenRulesData MultinoiseGenRules = new();
    public BiomeLegacyWorldGenRulesData LegacyWorldGenRules = new();
    public BiomeReplacementsData ReplacementBiomes = new();
    public VillageType VillageType;
    public BiomeSurfaceBuilderData SurfaceBuilderData = new();
    public BiomeSurfaceBuilderData SubsurfaceBuilderData = new();

    public void Read(BinaryReader reader) {
        Climate.Read(reader);
        ConsolidatedFeatures.Read(reader);
        MountainParams.Read(reader);
        SurfaceMaterialAdjustments.Read(reader);
        OverworldGenRules.Read(reader);
        MultinoiseGenRules.Read(reader);
        LegacyWorldGenRules.Read(reader);
        ReplacementBiomes.Read(reader);
        VillageType = (global::BedrockProtocol.Enums.VillageType)reader.ReadUInt8();
        SurfaceBuilderData.Read(reader);
        SubsurfaceBuilderData.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Climate.Write(writer);
        ConsolidatedFeatures.Write(writer);
        MountainParams.Write(writer);
        SurfaceMaterialAdjustments.Write(writer);
        OverworldGenRules.Write(writer);
        MultinoiseGenRules.Write(writer);
        LegacyWorldGenRules.Write(writer);
        ReplacementBiomes.Write(writer);
        writer.WriteUInt8((byte)VillageType);
        SurfaceBuilderData.Write(writer);
        SubsurfaceBuilderData.Write(writer);
    }
}
