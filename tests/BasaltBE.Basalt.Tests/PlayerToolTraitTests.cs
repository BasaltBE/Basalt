using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class PlayerToolTraitTests {
    [Fact]
    public void HoeTillsGrassBlockIntoFarmland() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        var trait = new PlayerToolTrait(player);
        var position = new BlockPos { X = 1, Y = -57, Z = 1 };
        BlockPermutation grass = BlockPalette.FindDefaultPermutation("minecraft:grass_block")!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, grass);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:wooden_hoe"), grass, position, 1, new Vec3());

        Assert.Equal("minecraft:farmland", fixture.Dimension.GetPermutation(position.X, position.Y, position.Z).Identifier);
    }

    [Fact]
    public void HoeTurnsCoarseDirtIntoDirt() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        var trait = new PlayerToolTrait(player);
        var position = new BlockPos { X = 1, Y = -57, Z = 1 };
        BlockPermutation coarseDirt = BlockPalette.FindDefaultPermutation("minecraft:coarse_dirt")!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, coarseDirt);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:wooden_hoe"), coarseDirt, position, 1, new Vec3());

        Assert.Equal("minecraft:dirt", fixture.Dimension.GetPermutation(position.X, position.Y, position.Z).Identifier);
    }

    [Fact]
    public void HoeTillsGrassPathIntoFarmland() {
        using MultiplayerFixture fixture = new();
        PlayerToolTrait trait = new(fixture.CreatePlayer());
        BlockPos position = new() { X = 1, Y = -57, Z = 1 };
        BlockPermutation grassPath = BlockPalette.FindDefaultPermutation("minecraft:grass_path")!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, grassPath);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:wooden_hoe"), grassPath, position, 1, new Vec3());

        Assert.Equal("minecraft:farmland", fixture.Dimension.GetPermutation(position.X, position.Y, position.Z).Identifier);
    }

    [Fact]
    public void ShovelCreatesGrassPathOnlyFromTopFace() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        var trait = new PlayerToolTrait(player);
        var position = new BlockPos { X = 1, Y = -57, Z = 1 };
        BlockPermutation grass = BlockPalette.FindDefaultPermutation("minecraft:grass_block")!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, grass);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:stone_shovel"), grass, position, 2, new Vec3());
        Assert.Same(grass, fixture.Dimension.GetPermutation(position.X, position.Y, position.Z));

        trait.OnUseItemOnBlock(new ItemStack("minecraft:stone_shovel"), grass, position, 1, new Vec3());
        Assert.Equal("minecraft:grass_path", fixture.Dimension.GetPermutation(position.X, position.Y, position.Z).Identifier);
    }

    [Theory]
    [InlineData("minecraft:dirt")]
    [InlineData("minecraft:coarse_dirt")]
    [InlineData("minecraft:podzol")]
    [InlineData("minecraft:mycelium")]
    [InlineData("minecraft:dirt_with_roots")]
    public void ShovelCreatesGrassPathFromDirtVariants(string identifier) {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        PlayerToolTrait trait = new(player);
        BlockPos position = new() { X = 1, Y = -57, Z = 1 };
        BlockPermutation block = BlockPalette.FindDefaultPermutation(identifier)!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, block);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:stone_shovel"), block, position, 1, new Vec3());

        Assert.Equal("minecraft:grass_path", fixture.Dimension.GetPermutation(position.X, position.Y, position.Z).Identifier);
    }

    [Fact]
    public void AxeStripsLogsAndPreservesAxis() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        var trait = new PlayerToolTrait(player);
        var position = new BlockPos { X = 1, Y = -57, Z = 1 };
        BlockPermutation log = BlockPalette.Find("minecraft:oak_log", BlockPalette.FindDefaultPermutation("minecraft:oak_log")!.State, "pillar_axis", "x")!;
        fixture.Dimension.SetPermutation(position.X, position.Y, position.Z, log);

        trait.OnUseItemOnBlock(new ItemStack("minecraft:wooden_axe"), log, position, 4, new Vec3());

        BlockPermutation stripped = fixture.Dimension.GetPermutation(position.X, position.Y, position.Z);
        Assert.Equal("minecraft:stripped_oak_log", stripped.Identifier);
        Assert.Equal("x", stripped.State.Values["pillar_axis"]);
    }
}

