using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class PlayerBlockBreakTests {
    [Theory]
    [InlineData(-1, true, 0)]
    [InlineData(0, false, 1)]
    [InlineData(0.01, false, 1)]
    [InlineData(0.5, false, 15)]
    [InlineData(0.5, true, 50)]
    [InlineData(1.5, true, 150)]
    [InlineData(100, true, 6000)]
    public void BreakDurationUsesHandSpeedAndCapsLongBreaks(float hardness, bool requiresTool, uint expected) {
        Assert.Equal(expected, PlayerAuthInputHandler.BreakDurationTicks(hardness, requiresTool));
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
