using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class PlayerBlockBreakTests {
    [Theory]
    [InlineData(-1, 1, true, 0)]
    [InlineData(0, 1, true, 1)]
    [InlineData(0.01, 1, true, 1)]
    [InlineData(0.5, 1, true, 15)]
    [InlineData(0.5, 1, false, 50)]
    [InlineData(1.5, 1, false, 150)]
    [InlineData(1.5, 4, true, 12)]
    [InlineData(0.6, 2, true, 9)]
    [InlineData(100, 1, false, 6000)]
    public void BreakDurationUsesMiningSpeedAndHarvestability(float hardness, float miningSpeed, bool canHarvest, uint expected) {
        Assert.Equal(expected, PlayerAuthInputHandler.BreakDurationTicks(hardness, miningSpeed, canHarvest));
    }

    [Fact]
    public void ToolPaletteProvidesTierSpeedsAndHarvestRequirements() {
        Assert.Equal(2, ItemPalette.Find("minecraft:wooden_pickaxe")!.MiningSpeed);
        Assert.Equal(4, ItemPalette.Find("minecraft:stone_pickaxe")!.MiningSpeed);
        Assert.Equal(5, ItemPalette.Find("minecraft:copper_pickaxe")!.MiningSpeed);
        Assert.Equal(6, ItemPalette.Find("minecraft:iron_pickaxe")!.MiningSpeed);
        Assert.Equal(12, ItemPalette.Find("minecraft:golden_pickaxe")!.MiningSpeed);
        Assert.Equal(8, ItemPalette.Find("minecraft:diamond_pickaxe")!.MiningSpeed);
        Assert.Equal(9, ItemPalette.Find("minecraft:netherite_pickaxe")!.MiningSpeed);
        Assert.Equal(3, ItemPalette.Find("minecraft:wooden_sword")!.AttackDamageBonus);
        Assert.Equal(4, ItemPalette.Find("minecraft:copper_sword")!.AttackDamageBonus);
        Assert.Equal(7, ItemPalette.Find("minecraft:netherite_sword")!.AttackDamageBonus);

        using MultiplayerFixture fixture = new();
        Assert.False(fixture.Server.LootTables.CanHarvest("minecraft:stone"));
        Assert.True(fixture.Server.LootTables.CanHarvest("minecraft:stone", new ItemStack("minecraft:wooden_pickaxe")));
        Assert.False(fixture.Server.LootTables.CanHarvest("minecraft:gold_ore", new ItemStack("minecraft:copper_pickaxe")));
    }

    [Fact]
    public void EveryToolFamilyUsesItsMaterialMiningSpeed() {
        (string Material, int Speed)[] materials = [
            ("wooden", 2),
            ("stone", 4),
            ("copper", 5),
            ("iron", 6),
            ("golden", 12),
            ("diamond", 8),
            ("netherite", 9)
        ];
        string[] families = ["pickaxe", "axe", "shovel", "hoe"];

        foreach ((string material, int speed) in materials) {
            foreach (string family in families) {
                ItemType tool = ItemPalette.Find($"minecraft:{material}_{family}")!;
                Assert.NotNull(tool);
                Assert.Equal(speed, tool.MiningSpeed);
            }
        }
    }

    [Fact]
    public void SwordAndAxeDamageBonusesMatchTheirMaterial() {
        (string Material, float Sword, float Axe)[] materials = [
            ("wooden", 3, 6),
            ("stone", 4, 8),
            ("copper", 4, 8),
            ("iron", 5, 8),
            ("golden", 3, 6),
            ("diamond", 6, 8),
            ("netherite", 7, 9)
        ];

        foreach ((string material, float swordDamage, float axeDamage) in materials) {
            Assert.Equal(swordDamage, ItemPalette.Find($"minecraft:{material}_sword")!.AttackDamageBonus);
            Assert.Equal(axeDamage, ItemPalette.Find($"minecraft:{material}_axe")!.AttackDamageBonus);
        }
    }

    [Fact]
    public void PredictDestroyBreaksBlockAtDurationMinusTolerance() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        player.Position.X = 0.5f;
        player.Position.Y = -57;
        player.Position.Z = 0.5f;
        player.AddTrait(new EntityMovementTrait(player));

        Chunk chunk = fixture.Dimension.GetOrCreateChunk(0, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        chunk.SetPermutation(1, -57, 0, stone);

        PlayerAuthInputHandler handler = new();
        handler.Handle(player.Peer, player, Input(1, PlayerActionType.StartDestroyBlock, 1, -57, 0));
        Assert.Equal(150U, player.BlockBreakDurationTicks);
        handler.Handle(player.Peer, player, Input(146, PlayerActionType.PredictDestroyBlock, 1, -57, 0));

        Assert.Same(BlockStorage.Air, fixture.Dimension.GetPermutation(1, -57, 0));
        Assert.Null(player.BreakingBlock);
    }

    [Fact]
    public void EarlyPredictionKeepsBlockAndClearsBreakState() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        player.Position.X = 0.5f;
        player.Position.Y = -57;
        player.Position.Z = 0.5f;
        player.AddTrait(new EntityMovementTrait(player));

        Chunk chunk = fixture.Dimension.GetOrCreateChunk(0, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        chunk.SetPermutation(1, -57, 0, stone);

        PlayerAuthInputHandler handler = new();
        handler.Handle(player.Peer, player, Input(1, PlayerActionType.StartDestroyBlock, 1, -57, 0));
        handler.Handle(player.Peer, player, Input(2, PlayerActionType.PredictDestroyBlock, 1, -57, 0));

        Assert.Same(stone, fixture.Dimension.GetPermutation(1, -57, 0));
        Assert.Null(player.BreakingBlock);
    }

    [Fact]
    public void PredictionOutsideReachDoesNotBreakLoadedBlock() {
        using MultiplayerFixture fixture = new();
        var player = fixture.CreatePlayer();
        player.Position.X = 0.5f;
        player.Position.Y = -57;
        player.Position.Z = 0.5f;
        player.AddTrait(new EntityMovementTrait(player));

        Chunk chunk = fixture.Dimension.GetOrCreateChunk(10, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        chunk.SetPermutation(1, -57, 0, stone);

        new PlayerAuthInputHandler().Handle(player.Peer, player,
            Input(1, PlayerActionType.PredictDestroyBlock, 161, -57, 0));

        Assert.Same(stone, fixture.Dimension.GetPermutation(161, -57, 0));
    }

    private static PlayerAuthInputPacket Input(ulong tick, PlayerActionType action, int x, int y, int z) => new() {
        Position = new Vec3 { X = 0.5f, Y = -55.38f, Z = 0.5f },
        ClientTick = new PlayerInputTick { Tick = tick },
        PlayerBlockActions = [new PlayerBlockActionData
        {
            PlayerActionType = action,
            Position = new BlockPos { X = x, Y = y, Z = z }
        }]
    };
}
