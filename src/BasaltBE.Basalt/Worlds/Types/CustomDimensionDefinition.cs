using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Worlds.Types;

public sealed class CustomDimensionDefinition {
    public readonly string Identifier;
    public readonly int MinimumY;
    public readonly int HeightRange;
    public readonly GeneratorType GeneratorType;
    public readonly string DefaultBiome;
    public readonly int DefaultBiomeId;
    public readonly int CloudHeight;
    public readonly bool RenderClouds;
    public readonly Guid PackId;
    public readonly int RuntimeId;

    internal CustomDimensionDefinition(
        string identifier,
        int minimumY,
        int heightRange,
        GeneratorType generatorType,
        string defaultBiome,
        int defaultBiomeId,
        int cloudHeight,
        bool renderClouds,
        Guid packId,
        int runtimeId
    ) {
        Identifier = identifier;
        MinimumY = minimumY;
        HeightRange = heightRange;
        GeneratorType = generatorType;
        DefaultBiome = defaultBiome;
        DefaultBiomeId = defaultBiomeId;
        CloudHeight = cloudHeight;
        RenderClouds = renderClouds;
        PackId = packId;
        RuntimeId = runtimeId;
    }
}
