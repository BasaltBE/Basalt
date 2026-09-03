namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class TreeCanopy {
    public readonly int OffsetMin;
    public readonly int OffsetMax;
    public readonly int RadiusMin;
    public readonly int RadiusMax;
    public readonly int RadiusStep;
    public readonly TreeBlock Block;
    public readonly TreeChance[] VariationChances;

    public TreeCanopy(
        int offsetMin,
        int offsetMax,
        int radiusMin,
        int radiusMax,
        int radiusStep,
        TreeBlock block,
        params TreeChance[] variationChances) {
        if (offsetMax < offsetMin) {
            throw new ArgumentOutOfRangeException(nameof(offsetMax));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(radiusMin);
        ArgumentOutOfRangeException.ThrowIfLessThan(radiusMax, radiusMin);
        ArgumentOutOfRangeException.ThrowIfLessThan(radiusStep, 1);

        OffsetMin = offsetMin;
        OffsetMax = offsetMax;
        RadiusMin = radiusMin;
        RadiusMax = radiusMax;
        RadiusStep = radiusStep;
        Block = block ?? throw new ArgumentNullException(nameof(block));
        VariationChances = variationChances is null ? [] : [.. variationChances];
    }
}
