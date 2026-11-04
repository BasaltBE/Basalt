using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Features;

public sealed class FeatureContext {
    public readonly Dimension Dimension;
    public readonly Random Random;
    readonly int capacity;
    internal readonly Dictionary<(int X, int Y, int Z), BlockPermutation> Blocks = [];
    internal bool Invalid;

    internal FeatureContext(Dimension dimension, Random random, int capacity) {
        Dimension = dimension;
        Random = random;
        this.capacity = capacity;
    }

    public BlockPermutation GetBlock(int x, int y, int z) => Blocks.GetValueOrDefault((x, y, z)) ?? Dimension.GetPermutation(x, y, z);

    public bool SetBlock(int x, int y, int z, BlockPermutation permutation) {
        ArgumentNullException.ThrowIfNull(permutation);
        if (y < Dimension.MinimumY || y > Dimension.MaximumY ||
            Blocks.Count >= capacity && !Blocks.ContainsKey((x, y, z))) {
            Invalid = true;
            return false;
        }
        Blocks[(x, y, z)] = permutation;
        return true;
    }
}
