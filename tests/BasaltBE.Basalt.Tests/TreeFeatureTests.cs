using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Features;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class TreeFeatureTests {
    static readonly FeatureRegistry Features = new();

    public static IEnumerable<object[]> Definitions() => Features.Identifiers
        .Where(identifier => Features.Find(identifier)!.Type == "minecraft:tree_feature")
        .Select(identifier => new object[] { identifier });

    [Theory]
    [MemberData(nameof(Definitions))]
    public void EveryTreeBuildsRepeatableGeometry(string identifier) {
        using Dimension dimension = Ground();
        Feature feature = Features.Find(identifier)!;
        var data = feature.Data.GetProperty(feature.Type);
        for (int seed = 0; seed < 20; seed++) {
            var first = new TreePlacement(data, dimension, new Random(seed)).Build(-1, 64, -1);
            var second = new TreePlacement(data, dimension, new Random(seed)).Build(-1, 64, -1);
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.InRange(first.Count, 1, 4096);
            Assert.Equal(first.Count, second.Count);
            Assert.Contains(first.Values, block => block.Kind == TreePlacement.BlockKind.Trunk);
            foreach (var block in first)
                Assert.Equal(block.Value, second[block.Key]);
            if (!data.TryGetProperty("fallen_trunk", out _))
                Assert.Contains(first.Values, block => block.Permutation.Identifier.EndsWith("_leaves", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("minecraft:pale_oak_tree_feature")]
    [InlineData("minecraft:roofed_tree_feature")]
    [InlineData("minecraft:mega_jungle_tree_feature")]
    [InlineData("minecraft:mega_pine_tree_feature")]
    [InlineData("minecraft:mega_spruce_tree_feature")]
    public void ThickTreesHaveFourTrunkColumns(string identifier) {
        using Dimension dimension = Ground();
        Feature feature = Features.Find(identifier)!;
        var blocks = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(0)).Build(0, 64, 0)!;
        for (int x = 0; x < 2; x++) {
            for (int z = 0; z < 2; z++) {
                Assert.Equal(TreePlacement.BlockKind.Trunk, blocks[(x, 64, z)].Kind);
                Assert.Equal(TreePlacement.BlockKind.Base, blocks[(x, 63, z)].Kind);
            }
        }
    }

    [Theory]
    [InlineData("minecraft:fancy_oak_tree_feature")]
    [InlineData("minecraft:cherry_tree_feature")]
    public void BranchingTreesHaveHorizontalLogs(string identifier) {
        using Dimension dimension = Ground();
        Feature feature = Features.Find(identifier)!;
        var blocks = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(0)).Build(0, 64, 0)!;
        Assert.Contains(blocks.Values, block => block.Kind == TreePlacement.BlockKind.Trunk &&
            block.Permutation.State.Values.TryGetValue("pillar_axis", out object? axis) && axis is "x" or "z");
        Assert.True(blocks.Keys.Max(position => position.X) - blocks.Keys.Min(position => position.X) > 4 ||
            blocks.Keys.Max(position => position.Z) - blocks.Keys.Min(position => position.Z) > 4);
    }

    [Theory]
    [InlineData("minecraft:spruce_tree_feature")]
    [InlineData("minecraft:mega_spruce_tree_feature")]
    public void SpruceUsesMoreThanFourLeafLayers(string identifier) {
        using Dimension dimension = Ground();
        Feature feature = Features.Find(identifier)!;
        var blocks = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(0)).Build(0, 64, 0)!;
        Assert.True(blocks.Where(block => block.Value.Permutation.Identifier == "minecraft:spruce_leaves")
            .Select(block => block.Key.Y).Distinct().Count() > 4);
    }

    [Fact]
    public void MangroveHasRootsAndHangingPropagules() {
        using Dimension dimension = Ground();
        Feature feature = Features.Find("minecraft:tall_mangrove_tree_feature")!;
        var blocks = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(0)).Build(0, 64, 0)!;
        Assert.Contains(blocks.Values, block => block.Kind == TreePlacement.BlockKind.Root);
        Assert.Contains(blocks.Values, block => block.Permutation.Identifier == "minecraft:mangrove_propagule" &&
            block.Permutation.State.Values["hanging"] is true);
    }

    [Fact]
    public void TreesCanReplaceLeavesAllowedByTheirDefinition() {
        using Dimension dimension = Ground();
        dimension.SetPermutation(0, 65, 0, BlockPalette.FindDefaultPermutation("minecraft:oak_leaves")!);
        Assert.True(Features.Place("minecraft:oak_tree_feature", dimension, 0, 64, 0, 0) > 0);
        Assert.Equal("minecraft:oak_log", dimension.GetPermutation(0, 65, 0).Identifier);
    }

    [Fact]
    public void NormalOakKeepsTheOldBasaltBroadleafGeometry() {
        using Dimension dimension = Ground();
        Feature feature = Features.Find("minecraft:oak_tree_feature")!;
        for (int seed = 0; seed < 50; seed++) {
            Random reference = new(seed);
            int height = reference.Next(4, 8);
            Dictionary<(int X, int Y, int Z), string> expected = [];
            expected[(0, 63, 0)] = "minecraft:dirt";
            for (int y = 0; y < height; y++)
                expected[(0, 64 + y, 0)] = "minecraft:oak_log";
            for (int offset = 0; offset >= -3; offset--) {
                int radius = Math.Max(1 - offset / 2, 0);
                for (int x = -radius; x <= radius; x++) {
                    for (int z = -radius; z <= radius; z++) {
                        if (Math.Abs(x) == radius && Math.Abs(z) == radius && (reference.Next(2) == 0 || offset == 0))
                            continue;
                        expected.TryAdd((x, 64 + height + offset, z), "minecraft:oak_leaves");
                    }
                }
            }
            var actual = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(seed)).Build(0, 64, 0)!;
            Assert.Equal(expected.Count, actual.Count);
            foreach (var block in expected)
                Assert.Equal(block.Value, actual[block.Key].Permutation.Identifier);
        }
    }

    [Fact]
    public void FailedPlacementLeavesTheWorldUnchanged() {
        using Dimension dimension = Ground();
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation dirt = dimension.GetPermutation(0, 63, 0);
        dimension.SetPermutation(0, 65, 0, stone);
        Assert.Equal(0, Features.Place("minecraft:oak_tree_feature", dimension, 0, 64, 0, 0));
        Assert.Equal(stone, dimension.GetPermutation(0, 65, 0));
        Assert.Equal(dirt, dimension.GetPermutation(0, 63, 0));
        Assert.Equal("minecraft:air", dimension.GetPermutation(0, 64, 0).Identifier);

        dimension.SetPermutation(16, 317, 0, dirt);
        Assert.Equal(0, Features.Place("minecraft:cherry_tree_feature", dimension, 16, 318, 0, 0));
        Assert.Equal("minecraft:air", dimension.GetPermutation(16, 318, 0).Identifier);
        Assert.Equal(dirt, dimension.GetPermutation(16, 317, 0));
    }

    [Fact]
    public void PublicPlacementCommitsTheGeneratedPermutations() {
        using Dimension dimension = Ground();
        Feature feature = Features.Find("minecraft:cherry_tree_feature")!;
        var expected = new TreePlacement(feature.Data.GetProperty(feature.Type), dimension, new Random(17)).Build(0, 64, 0)!;
        Assert.True(Features.Place(feature.Identifier, dimension, 0, 64, 0, 17) > 0);
        foreach (var block in expected)
            Assert.Equal(block.Value.Permutation, dimension.GetPermutation(block.Key.X, block.Key.Y, block.Key.Z));
    }

    static Dimension Ground() {
        Dimension dimension = new("tree-test", DimensionType.Overworld, new VoidGenerator());
        BlockPermutation dirt = BlockPalette.FindDefaultPermutation("minecraft:dirt")!;
        for (int x = -24; x <= 24; x++) {
            for (int z = -24; z <= 24; z++)
                dimension.SetPermutation(x, 63, z, dirt);
        }
        return dimension;
    }
}
