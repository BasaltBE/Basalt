namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class TreeTrunk {
    private readonly int _heightBase;
    private readonly int _heightRandomA;
    private readonly int _heightRandomB;
    private readonly bool _splitRandom;

    public readonly int HeightMin;
    public readonly int HeightMax;
    public readonly TreeBlock Block;

    public TreeTrunk(int heightMin, int heightMax, TreeBlock block) {
        ArgumentOutOfRangeException.ThrowIfLessThan(heightMin, 1);

        if (heightMax < heightMin) {
            throw new ArgumentOutOfRangeException(nameof(heightMax));
        }

        HeightMin = heightMin;
        HeightMax = heightMax;
        Block = block ?? throw new ArgumentNullException(nameof(block));
        _heightBase = heightMin;
        _heightRandomA = heightMax - heightMin;
    }

    internal TreeTrunk(
        int heightBase,
        int heightRandomA,
        int heightRandomB,
        TreeBlock block) {
        ArgumentOutOfRangeException.ThrowIfLessThan(heightBase, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(heightRandomA);
        ArgumentOutOfRangeException.ThrowIfNegative(heightRandomB);

        _heightBase = heightBase;
        _heightRandomA = heightRandomA;
        _heightRandomB = heightRandomB;
        _splitRandom = true;
        HeightMin = heightBase;
        HeightMax = heightBase + heightRandomA + heightRandomB;
        Block = block ?? throw new ArgumentNullException(nameof(block));
    }

    internal int Sample(Random random) {
        if (!_splitRandom) {
            return random.Next(HeightMin, HeightMax + 1);
        }

        return _heightBase +
            random.Next(_heightRandomA + 1) +
            random.Next(_heightRandomB + 1);
    }
}
