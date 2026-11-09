using BasaltBE.Basalt.Blocks;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class BlockTests {
    [Fact]
    public void SetStatesFindsPermutationForStringState() {
        Block block = new("minecraft:dark_oak_wood");

        Assert.Equal("y", block.Permutation.State.Values["pillar_axis"]);
        Assert.True(block.SetStates("pillar_axis", "x"));
        Assert.Equal("x", block.Permutation.State.Values["pillar_axis"]);
    }

    [Fact]
    public void SetStatesFindsPermutationForNumericStateAndKeepsOtherStates() {
        Block block = new("minecraft:blue_candle");

        Assert.True(block.SetStates("candles", 2));

        Assert.Equal(2, block.Permutation.State.Values["candles"]);
        Assert.Equal(false, block.Permutation.State.Values["lit"]);
    }

    [Fact]
    public void SetStatesKeepsCurrentPermutationWhenStateDoesNotExist() {
        Block block = new("minecraft:dark_oak_wood");
        var permutation = block.Permutation;

        Assert.False(block.SetStates("pillar_axis", "diagonal"));
        Assert.Same(permutation, block.Permutation);
    }
}
