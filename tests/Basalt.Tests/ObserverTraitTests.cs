namespace Basalt.Tests;

using Basalt.BedrockProtocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Traits.Types;

public sealed class ObserverTraitTests {
    [Fact]
    public void PoweredObserverOnlyPowersBehindItsFace() {
        BlockState state = [];
        state["minecraft:facing_direction"] = "west";
        state["powered_bit"] = true;
        BlockPermutation observer = BlockPermutation.Resolve(BlockIdentifier.Observer, state);
        BlockPos position = new() { X = 0, Y = 0, Z = 0 };

        Assert.Equal("minecraft:observer", observer.Type.Identifier);
        Assert.Equal("west", observer.State["minecraft:facing_direction"].AsString());
        Assert.True(observer.State["powered_bit"].AsBool());
        Assert.True(ObserverTrait.Powers(observer, position, new BlockPos { X = 1, Y = 0, Z = 0 }));
        Assert.False(ObserverTrait.Powers(observer, position, new BlockPos { X = -1, Y = 0, Z = 0 }));
        Assert.False(ObserverTrait.Powers(observer, position, new BlockPos { X = 0, Y = 1, Z = 0 }));
    }

    [Fact]
    public void UnpoweredObserverDoesNotPower() {
        BlockState state = [];
        state["minecraft:facing_direction"] = "north";
        state["powered_bit"] = false;
        BlockPermutation observer = BlockPermutation.Resolve(BlockIdentifier.Observer, state);

        Assert.False(ObserverTrait.Powers(
            observer,
            new BlockPos { X = 0, Y = 0, Z = 0 },
            new BlockPos { X = 0, Y = 0, Z = 1 }));
    }

    [Fact]
    public void ObserverBlockUsesObserverTrait() {
        Assert.Contains(
            typeof(ObserverTrait),
            BlockPalette.ResolveType(BlockIdentifier.Observer).Traits.Values);
    }
}
