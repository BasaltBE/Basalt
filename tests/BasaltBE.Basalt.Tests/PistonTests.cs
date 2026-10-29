using System.Diagnostics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using Xunit;
using Xunit.Abstractions;

namespace BasaltBE.Basalt.Tests;

[Collection("Block trait registrations")]
public sealed class PistonTests(ITestOutputHelper output) {
    static int neighborProbeRegistered;
    static int pistonTraitRegistered;
    static int scheduledPoppedTraitRegistered;

    [Fact]
    public void ExtensionAndRetractionCommitTheHeadAndMovedBlockTogether() {
        using Dimension dimension = new("test:piston-movement", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation head = BlockPalette.Find("minecraft:piston_arm_collision", BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State, "facing_direction", 4)!;
        dimension.SetPermutation(1, 100, 0, stone);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(1, 100, 0)],
            (1, 0, 0),
            [],
            [(1, 100, 0, head)],
            actor,
            true
        ));
        Assert.Equal("minecraft:piston_arm_collision", dimension.GetPermutation(1, 100, 0).Identifier);
        Assert.Same(stone, dimension.GetPermutation(2, 100, 0));
        Assert.True(actor.Extended);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(2, 100, 0)],
            (-1, 0, 0),
            [],
            [(1, 100, 0, BlockStorage.Air)],
            actor,
            false
        ));
        Assert.Same(stone, dimension.GetPermutation(1, 100, 0));
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(2, 100, 0));
        Assert.False(actor.Extended);
    }

    [Fact]
    public void PoweredPistonPushesTheBlockInFrontOfItsHead() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-powered-extension", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension, 8, 8);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation lever = BlockPalette.Find(
            "minecraft:lever",
            BlockPalette.FindDefaultPermutation("minecraft:lever")!.State,
            "open_bit",
            false
        )!;
        dimension.SetPermutation(7, 100, 8, lever);
        dimension.SetPermutation(9, 100, 8, stone);
        dimension.SetPermutation(7, 100, 8, BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!);
        dimension.UpdateNeighbors(7, 100, 8);

        Assert.Equal("minecraft:piston_arm_collision", dimension.GetPermutation(9, 100, 8).Identifier);
        Assert.Same(stone, dimension.GetPermutation(10, 100, 8));
        Assert.True(actor.Extended);
    }

    [Fact]
    public void BlockedExtensionDoesNotMutateAnyBlockOrActor() {
        using Dimension dimension = new("test:piston-blocked", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation obsidian = BlockPalette.FindDefaultPermutation("minecraft:obsidian")!;
        BlockPermutation head = BlockPalette.Find("minecraft:piston_arm_collision", BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State, "facing_direction", 4)!;
        dimension.SetPermutation(1, 100, 0, stone);
        dimension.SetPermutation(2, 100, 0, obsidian);

        Assert.Equal(PistonTransitionResult.Rejected, dimension.MovePistonBlocks(
            [(1, 100, 0)],
            (1, 0, 0),
            [],
            [(1, 100, 0, head)],
            actor,
            true
        ));

        Assert.Same(stone, dimension.GetPermutation(1, 100, 0));
        Assert.Same(obsidian, dimension.GetPermutation(2, 100, 0));
        Assert.False(actor.Extended);
    }

    [Fact]
    public void DestroyedBlockAtHeadCanBeReplacedByPistonHead() {
        using Dimension dimension = new("test:piston-destroy-head", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation poppy = BlockPalette.FindDefaultPermutation("minecraft:poppy")!;
        BlockPermutation head = BlockPalette.Find("minecraft:piston_arm_collision", BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State, "facing_direction", 4)!;
        dimension.SetPermutation(1, 100, 0, poppy);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [],
            default,
            [(1, 100, 0)],
            [(1, 100, 0, head)],
            actor,
            true
        ));

        Assert.Equal("minecraft:piston_arm_collision", dimension.GetPermutation(1, 100, 0).Identifier);
        Assert.True(actor.Extended);
    }

    [Fact]
    public void PoppedBlockCancelsItsScheduledTickWhenDestroyed() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();
        if (Interlocked.Exchange(ref scheduledPoppedTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledPoppedTrait>(["minecraft:short_grass"]);

        using Dimension dimension = new("test:piston-scheduled-pop", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation shortGrass = BlockPalette.FindDefaultPermutation("minecraft:short_grass")!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(1, 100, 0, shortGrass);
        dimension.ScheduleBlockUpdate(1, 100, 0, 20);
        Assert.True(dimension.HasPendingBlockUpdate(1, 100, 0));

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [],
            default,
            [(1, 100, 0)],
            [(1, 100, 0, head)],
            actor,
            true
        ));

        Assert.False(dimension.HasPendingBlockUpdate(1, 100, 0));
        Assert.Equal("minecraft:piston_arm_collision", dimension.GetPermutation(1, 100, 0).Identifier);
    }

    [Fact]
    public void MovedObserverTransfersPendingTicksAcrossChunkUsingDestinationClock() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-scheduled-move", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension, 13);
        Chunk destinationChunk = dimension.GetOrCreateChunk(1, 0);
        destinationChunk.PendingTicks = new CompoundTag();
        destinationChunk.PendingTicks.Add("currentTick", new IntTag { Value = 10 });
        destinationChunk.PendingTicks.Add("tickList", new ListTag { ElementType = BasaltBE.Nbt.Enums.TagType.Compound });
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation poweredObserver = BlockPalette.Find("minecraft:observer", observer.State, "powered_bit", true)!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(15, 100, 0, observer);
        dimension.ScheduleBlockUpdate(15, 100, 0, 20, observer);
        dimension.ScheduleBlockUpdate(15, 100, 0, 20, poweredObserver);
        dimension.ScheduleBlockUpdate(15, 100, 0, 20, observer);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(15, 100, 0)],
            (1, 0, 0),
            [],
            [(14, 100, 0, head)],
            actor,
            true
        ));

        Assert.False(dimension.HasPendingBlockUpdate(15, 100, 0));
        Assert.True(dimension.HasPendingBlockUpdate(16, 100, 0));
        ListTag movedTicks = destinationChunk.PendingTicks!.Get<ListTag>("tickList")!;
        Assert.Equal(3, movedTicks.Value.Count);
        foreach (CompoundTag movedTick in movedTicks.Value.Cast<CompoundTag>()) {
            Assert.Equal(16, movedTick.Get<IntTag>("x")!.Value);
            Assert.Equal(100, movedTick.Get<IntTag>("y")!.Value);
            Assert.Equal(0, movedTick.Get<IntTag>("z")!.Value);
            Assert.Equal(30, movedTick.Get<LongTag>("time")!.Value);
        }
    }

    [Fact]
    public void StickyPistonPullsObserverAndItsScheduledTicksAcrossChunkBoundary() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-pull-scheduled-observer", new VoidGenerator());
        BlockPermutation piston = BlockPalette.Find(
            "minecraft:sticky_piston",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston")!.State,
            "facing_direction", 4
        )!;
        Chunk baseChunk = dimension.GetOrCreateChunk(0, 0);
        baseChunk.SetPermutation(14, 100, 0, piston);
        PistonArmBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = 14, Y = 100, Z = 0 },
            "minecraft:sticky_piston",
            piston,
            new CompoundTag()
        )) { State = 2, Sticky = true };
        baseChunk.SetRuntimeActor(14, 100, 0, actor);
        BlockPermutation head = BlockPalette.Find(
            "minecraft:sticky_piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(15, 100, 0, head);
        for (int chunkX = -1; chunkX <= 2; chunkX++) {
            for (int chunkZ = -1; chunkZ <= 1; chunkZ++)
                dimension.GetOrCreateChunk(chunkX, chunkZ);
        }
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        dimension.SetPermutation(16, 100, 0, observer);
        dimension.ScheduleBlockUpdate(16, 100, 0, 20);

        Assert.True(BlockPalette.OnNeighborUpdate(dimension, 14, 100, 0));

        Assert.False(actor.Extended);
        Assert.Equal("minecraft:observer", dimension.GetPermutation(15, 100, 0).Identifier);
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(16, 100, 0));
        Assert.False(dimension.HasPendingBlockUpdate(16, 100, 0));
        Assert.True(dimension.HasPendingBlockUpdate(15, 100, 0));
        CompoundTag movedTick = Assert.IsType<CompoundTag>(Assert.Single(
            baseChunk.PendingTicks!.Get<ListTag>("tickList")!.Value
        ));
        Assert.Equal(15, movedTick.Get<IntTag>("x")!.Value);
        Assert.Equal(20, movedTick.Get<LongTag>("time")!.Value);
    }

    [Fact]
    public void BlockWithUnindexedSavedTicksCannotBeMoved() {
        using Dimension dimension = new("test:piston-unindexed-tick", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        Chunk chunk = dimension.GetChunk(0, 0)!;
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 0 });
        CompoundTag blockState = new();
        blockState.Add("name", new StringTag { Value = "minecraft:unknown_scheduled_block" });
        CompoundTag record = new();
        record.Add("blockState", blockState);
        record.Add("time", new LongTag { Value = 20 });
        record.Add("x", new IntTag { Value = 1 });
        record.Add("y", new IntTag { Value = 100 });
        record.Add("z", new IntTag { Value = 0 });
        pending.Add("tickList", new ListTag {
            ElementType = BasaltBE.Nbt.Enums.TagType.Compound,
            Value = [record]
        });
        chunk.PendingTicks = pending;
        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.SetPermutation(1, 100, 0, stone);

        Assert.Equal(PistonTransitionResult.Rejected, dimension.MovePistonBlocks(
            [(1, 100, 0)],
            (1, 0, 0),
            [],
            [],
            actor,
            true
        ));

        Assert.Same(stone, dimension.GetPermutation(1, 100, 0));
    }

    [Fact]
    public void ChunkLoadSchedulesPistonsInLoadedAdjacentChunks() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-chunk-load", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension, 15);
        actor.Update();
        dimension.GetChunk(0, 0)!.SetBlockNbt(15, 100, 0, actor.Nbt);
        Chunk newlyLoaded = dimension.GetOrCreateChunk(1, 0);
        dimension.Redstone.ChunkLoaded(dimension, newlyLoaded);

        Assert.True(dimension.HasPendingBlockUpdate(15, 100, 0));
    }

    [Fact]
    public void ChunkLoadSchedulesPistonsAcrossDiagonalChunkBoundaries() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-diagonal-chunk-load", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension, 15, 15);
        actor.Update();
        dimension.GetChunk(0, 0)!.SetBlockNbt(15, 100, 15, actor.Nbt);
        Chunk newlyLoaded = dimension.GetOrCreateChunk(1, 1);
        dimension.Redstone.ChunkLoaded(dimension, newlyLoaded);

        Assert.True(dimension.HasPendingBlockUpdate(15, 100, 15));
    }

    [Fact]
    public void MissingPistonChunkSchedulesItsExactWaitingPistonWhenLoaded() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-missing-chunk", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        actor.Update();
        dimension.GetChunk(0, 0)!.SetBlockNbt(0, 100, 0, actor.Nbt);
        dimension.Redstone.WaitForPiston(0, 100, 0, 5, -4);
        Chunk newlyLoaded = dimension.GetOrCreateChunk(5, -4);

        dimension.Redstone.ChunkLoaded(dimension, newlyLoaded);

        Assert.True(dimension.HasPendingBlockUpdate(0, 100, 0));
    }

    [Fact]
    public void RemovingPistonClearsItsMissingChunkWait() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-remove-wait", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        actor.Update();
        dimension.GetChunk(0, 0)!.SetBlockNbt(0, 100, 0, actor.Nbt);
        dimension.Redstone.WaitForPiston(0, 100, 0, 5, 0);

        dimension.SetPermutation(0, 100, 0, BlockStorage.Air);
        dimension.Redstone.ChunkLoaded(dimension, dimension.GetOrCreateChunk(5, 0));

        Assert.False(dimension.HasPendingBlockUpdate(0, 100, 0));
    }

    [Fact]
    public void StickyPistonRetractsWithoutPullWhenAnAttachedDestinationIsBlocked() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-blocked-pull", new VoidGenerator());
        BlockPermutation piston = BlockPalette.Find(
            "minecraft:sticky_piston",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston")!.State,
            "facing_direction", 4
        )!;
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        chunk.SetPermutation(8, 100, 8, piston);
        PistonArmBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = 8, Y = 100, Z = 8 },
            "minecraft:sticky_piston",
            piston,
            new CompoundTag()
        )) { State = 2, Sticky = true };
        chunk.SetRuntimeActor(8, 100, 8, actor);
        BlockPermutation head = BlockPalette.Find(
            "minecraft:sticky_piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(9, 100, 8, head);
        dimension.SetPermutation(10, 100, 8, BlockPalette.FindDefaultPermutation("minecraft:slime")!);
        dimension.SetPermutation(10, 101, 8, BlockPalette.FindDefaultPermutation("minecraft:short_grass")!);
        dimension.SetPermutation(10, 100, 9, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        dimension.SetPermutation(9, 100, 9, BlockPalette.FindDefaultPermutation("minecraft:obsidian")!);

        Assert.True(BlockPalette.OnNeighborUpdate(dimension, 8, 100, 8));

        Assert.False(actor.Extended);
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(9, 100, 8));
        Assert.Equal("minecraft:slime", dimension.GetPermutation(10, 100, 8).Identifier);
        Assert.Equal("minecraft:short_grass", dimension.GetPermutation(10, 101, 8).Identifier);
        Assert.Equal("minecraft:stone", dimension.GetPermutation(10, 100, 9).Identifier);
    }

    [Fact]
    public void StickyPistonBreaksPoppedSideBranchAndStillPullsAttachedBlock() {
        if (Interlocked.Exchange(ref pistonTraitRegistered, 1) == 0)
            BlockPalette.RegisterTrait<PistonTrait>();

        using Dimension dimension = new("test:piston-pull-popped-branch", new VoidGenerator());
        BlockPermutation piston = BlockPalette.Find(
            "minecraft:sticky_piston",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston")!.State,
            "facing_direction", 4
        )!;
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        chunk.SetPermutation(8, 100, 8, piston);
        PistonArmBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = 8, Y = 100, Z = 8 },
            "minecraft:sticky_piston",
            piston,
            new CompoundTag()
        )) { State = 2, Sticky = true };
        chunk.SetRuntimeActor(8, 100, 8, actor);
        BlockPermutation head = BlockPalette.Find(
            "minecraft:sticky_piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:sticky_piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(9, 100, 8, head);
        dimension.SetPermutation(10, 100, 8, BlockPalette.FindDefaultPermutation("minecraft:slime")!);
        dimension.SetPermutation(10, 101, 8, BlockPalette.FindDefaultPermutation("minecraft:short_grass")!);

        Assert.True(BlockPalette.OnNeighborUpdate(dimension, 8, 100, 8));

        Assert.False(actor.Extended);
        Assert.Equal("minecraft:slime", dimension.GetPermutation(9, 100, 8).Identifier);
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(10, 100, 8));
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(10, 101, 8));
    }

    [Fact]
    public void RemovingExtendedPistonBaseAlsoRemovesItsHead() {
        using Dimension dimension = new("test:piston-base-removal", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        Chunk chunk = dimension.GetChunk(0, 0)!;
        actor.State = 2;
        chunk.SetBlockNbt(0, 100, 0, actor.Nbt);
        chunk.SetPermutation(1, 100, 0, head);

        dimension.SetPermutation(0, 100, 0, BlockStorage.Air);

        Assert.Same(BlockStorage.Air, dimension.GetPermutation(1, 100, 0));
        Assert.Null(chunk.GetBlockNbt(0, 100, 0));
    }

    [Fact]
    public void MovedFenceRecalculatesItsConnectionsAtTheDestination() {
        using Dimension dimension = new("test:piston-fence", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation fence = BlockPalette.FindDefaultPermutation("minecraft:oak_fence")!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(1, 100, -1, fence);
        dimension.SetPermutation(1, 100, 0, fence);
        BlockPalette.OnNeighborUpdate(dimension, 1, 100, 0);
        Assert.True((bool)dimension.GetPermutation(1, 100, 0).State.Values["minecraft:connection_north"]!);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(1, 100, 0)],
            (1, 0, 0),
            [],
            [(1, 100, 0, head)],
            actor,
            true
        ));

        Assert.False((bool)dimension.GetPermutation(2, 100, 0).State.Values["minecraft:connection_north"]!);
    }

    [Fact]
    public void ReplaceablePoppedSideBranchUpdatesItsNeighbors() {
        if (Interlocked.Exchange(ref neighborProbeRegistered, 1) == 0)
            BlockPalette.RegisterTrait<NeighborUpdateProbeTrait>(["minecraft:oak_fence"]);

        using Dimension dimension = new("test:piston-popped-branch", new VoidGenerator());
        PistonArmBlockActor actor = CreatePiston(dimension);
        BlockPermutation slime = BlockPalette.FindDefaultPermutation("minecraft:slime")!;
        BlockPermutation shortGrass = BlockPalette.FindDefaultPermutation("minecraft:short_grass")!;
        BlockPermutation fence = BlockPalette.FindDefaultPermutation("minecraft:oak_fence")!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.SetPermutation(1, 100, 0, slime);
        dimension.SetPermutation(1, 100, 1, shortGrass);
        dimension.SetPermutation(1, 100, 2, fence);
        NeighborUpdateProbeTrait.Calls = 0;

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(1, 100, 0)],
            (1, 0, 0),
            [(1, 100, 1)],
            [(1, 100, 0, head)],
            actor,
            true
        ));

        Assert.Same(BlockStorage.Air, dimension.GetPermutation(1, 100, 1));
        Assert.Equal(1, NeighborUpdateProbeTrait.Calls);
    }

    [Fact]
    public void CrossChunkTransitionUsesOneAtomicProviderWrite() {
        using Server server = new(new ServerProperties { DefaultWorldName = "test-piston-storage" });
        server.Generators.Register<VoidGenerator>("void");
        server.Generators.SetDefault("void");
        RecordingProvider provider = new();
        World world = server.CreateWorld(
            "test-piston-storage",
            configure => configure.CreateDimension("minecraft:overworld"),
            provider
        );
        Dimension dimension = world.GetDimension("minecraft:overworld")!;
        PistonArmBlockActor actor = CreatePiston(dimension, 14);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;
        dimension.GetOrCreateChunk(1, 0);
        dimension.SetPermutation(15, 100, 0, stone);

        Assert.Equal(PistonTransitionResult.Committed, dimension.MovePistonBlocks(
            [(15, 100, 0)],
            (1, 0, 0),
            [],
            [(15, 100, 0, head)],
            actor,
            true
        ));
        Assert.True(dimension.FlushStorage());

        Assert.Equal([2], provider.BatchSizes);
        Assert.NotNull(provider.ReadChunk(new ChunkStorageKey(
            DimensionType.Overworld,
            "minecraft:overworld",
            true,
            0,
            0
        )));
        Assert.NotNull(provider.ReadChunk(new ChunkStorageKey(
            DimensionType.Overworld,
            "minecraft:overworld",
            true,
            1,
            0
        )));
    }

    [Fact]
    public void BenchmarkStablePistonTransitions() {
        const int warmupCount = 30;
        const int sampleCount = 250;
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation head = BlockPalette.Find("minecraft:piston_arm_collision", BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State, "facing_direction", 4)!;

        foreach (int blockCount in new[] { 1, 12 }) {
            using Dimension dimension = new($"test:piston-benchmark-{blockCount}", new VoidGenerator());
            PistonArmBlockActor actor = CreatePiston(dimension);
            (int X, int Y, int Z)[] extension = Enumerable.Range(1, blockCount).Select(static x => (x, 100, 0)).ToArray();
            (int X, int Y, int Z)[] retraction = Enumerable.Range(2, blockCount).Select(static x => (x, 100, 0)).ToArray();
            for (int x = 1; x <= blockCount; x++)
                dimension.SetPermutation(x, 100, 0, stone);

            double[] samples = new double[sampleCount];
            long allocated = 0;
            for (int sample = -warmupCount; sample < sampleCount; sample++) {
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                PistonTransitionResult extensionResult = dimension.MovePistonBlocks(
                    extension,
                    (1, 0, 0),
                    [],
                    [(1, 100, 0, head)],
                    actor,
                    true
                );
                PistonTransitionResult retractionResult = dimension.MovePistonBlocks(
                    retraction,
                    (-1, 0, 0),
                    [],
                    [(1, 100, 0, BlockStorage.Air)],
                    actor,
                    false
                );
                double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                long sampleAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                Assert.Equal(PistonTransitionResult.Committed, extensionResult);
                Assert.Equal(PistonTransitionResult.Committed, retractionResult);
                if (sample >= 0) {
                    samples[sample] = elapsed;
                    allocated += sampleAllocated;
                }
            }

            Array.Sort(samples);
            output.WriteLine(
                $"Stable piston commit, {blockCount} moved block(s), {sampleCount} samples, pair p50 {Percentile(samples, 0.50):F4} ms, p95 {Percentile(samples, 0.95):F4} ms, p99 {Percentile(samples, 0.99):F4} ms, max {samples[^1]:F4} ms, {allocated / (double)sampleCount / 2:F0} bytes per transition."
            );
        }
    }

    [Fact]
    public void BenchmarkPistonScheduledTickTransfersAcrossChunkBoundary() {
        const int warmupCount = 30;
        const int sampleCount = 250;
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation poweredObserver = BlockPalette.Find("minecraft:observer", observer.State, "powered_bit", true)!;
        BlockPermutation head = BlockPalette.Find(
            "minecraft:piston_arm_collision",
            BlockPalette.FindDefaultPermutation("minecraft:piston_arm_collision")!.State,
            "facing_direction", 4
        )!;

        foreach (int pendingTickCount in new[] { 1, 3, 10, 100 }) {
            using Dimension dimension = new($"test:piston-scheduled-benchmark-{pendingTickCount}", new VoidGenerator());
            PistonArmBlockActor actor = CreatePiston(dimension, 13);
            Chunk destinationChunk = dimension.GetOrCreateChunk(1, 0);
            destinationChunk.PendingTicks = new CompoundTag();
            destinationChunk.PendingTicks.Add("currentTick", new IntTag { Value = 10 });
            destinationChunk.PendingTicks.Add("tickList", new ListTag { ElementType = BasaltBE.Nbt.Enums.TagType.Compound });
            dimension.SetPermutation(15, 100, 0, observer);
            for (int index = 0; index < pendingTickCount; index++) {
                BlockPermutation scheduled = index % 2 == 0 ? observer : poweredObserver;
                dimension.ScheduleBlockUpdate(15, 100, 0, 20, scheduled);
            }
            Assert.Equal(
                pendingTickCount,
                dimension.GetChunk(0, 0)!.PendingTicks!.Get<ListTag>("tickList")!.Value.Count
            );
            double[] samples = new double[sampleCount];
            long allocated = 0;

            for (int sample = -warmupCount; sample < sampleCount; sample++) {
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                PistonTransitionResult extensionResult = dimension.MovePistonBlocks(
                    [(15, 100, 0)],
                    (1, 0, 0),
                    [],
                    [(14, 100, 0, head)],
                    actor,
                    true
                );
                PistonTransitionResult retractionResult = dimension.MovePistonBlocks(
                    [(16, 100, 0)],
                    (-1, 0, 0),
                    [],
                    [(14, 100, 0, BlockStorage.Air)],
                    actor,
                    false
                );
                double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                long sampleAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                Assert.Equal(PistonTransitionResult.Committed, extensionResult);
                Assert.Equal(PistonTransitionResult.Committed, retractionResult);
                if (sample >= 0) {
                    samples[sample] = elapsed;
                    allocated += sampleAllocated;
                }
            }

            Array.Sort(samples);
            output.WriteLine(
                $"Scheduled piston commit, 1 moved observer with {pendingTickCount} pending tick(s) across a chunk boundary, {sampleCount} samples, pair p50 {Percentile(samples, 0.50):F4} ms, p95 {Percentile(samples, 0.95):F4} ms, p99 {Percentile(samples, 0.99):F4} ms, max {samples[^1]:F4} ms, {allocated / (double)sampleCount / 2:F0} bytes per transition."
            );
        }
    }

    static PistonArmBlockActor CreatePiston(Dimension dimension, int x = 0, int z = 0) {
        BlockPermutation piston = BlockPalette.Find("minecraft:piston", BlockPalette.FindDefaultPermutation("minecraft:piston")!.State, "facing_direction", 4)!;
        Chunk chunk = dimension.GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetPermutation(x & 15, 100, z & 15, piston);
        PistonArmBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = x, Y = 100, Z = z },
            "minecraft:piston",
            piston,
            new CompoundTag()
        ));
        chunk.SetRuntimeActor(x & 15, 100, z & 15, actor);
        return actor;
    }

    static double Percentile(double[] sorted, double percentile) =>
        sorted[(int)Math.Ceiling((sorted.Length - 1) * percentile)];

    [BlockTraitAppliesTo("minecraft:oak_fence")]
    public sealed class NeighborUpdateProbeTrait : BlockTrait {
        public static int Calls;

        public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
            Interlocked.Increment(ref Calls);
            return false;
        }
    }

    [BlockTraitAppliesTo("minecraft:short_grass")]
    public sealed class ScheduledPoppedTrait : BlockTrait {
        internal override bool HasScheduledUpdates => true;
    }

    sealed class RecordingProvider : IWorldProvider {
        readonly Dictionary<ChunkStorageKey, ChunkStorageEntry[]> chunks = [];
        readonly Lock sync = new();
        readonly List<int> batchSizes = [];

        public string Identifier => "recording";
        public int[] BatchSizes {
            get {
                lock (sync)
                    return [.. batchSizes];
            }
        }

        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
            lock (sync)
                return chunks.TryGetValue(key, out ChunkStorageEntry[]? entries) ? entries.ToArray() : null;
        }

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> records) {
            lock (sync) {
                batchSizes.Add(records.Length);
                foreach (ChunkStorageRecord record in records)
                    chunks[record.Key] = record.Entries.ToArray();
            }
        }

        public bool DeleteChunk(ChunkStorageKey key) {
            lock (sync)
                return chunks.Remove(key);
        }

        public void Flush() { }
        public void Dispose() { }
    }
}
