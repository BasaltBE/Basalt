using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Channels;
using Basalt.Binary;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Connections;
using BasaltBE.NetherNet.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using Xunit;
using Xunit.Abstractions;

namespace BasaltBE.Basalt.Tests;

[CollectionDefinition("Block trait registrations", DisableParallelization = true)]
public sealed class BlockTraitRegistrationCollection : ICollectionFixture<BlockTraitRegistrationFixture> { }

public sealed class BlockTraitRegistrationFixture {
    public BlockTraitRegistrationFixture() {
        BlockPalette.RegisterTrait<ButtonTrait>(["minecraft:stone_button"]);
        BlockPalette.RegisterTrait<FenceTrait>();
        BlockPalette.RegisterTrait<ObserverTrait>();
    }
}

[Collection("Block trait registrations")]
public sealed class ScheduledBlockTickTests(ITestOutputHelper output) {
    static int registered;
    static int redstoneDevicesRegistered;
    static int redstoneWireRegistered;
    static int daylightDetectorRegistered;
    static int leverRegistered;
    static int manyChunkBenchmarkRegistered;

    [Fact]
    public void ObserverPulseSurvivesChunkSaveAndRestart() {

        using Dimension original = new("test:observer-original", new VoidGenerator());
        Chunk originalChunk = original.GetOrCreateChunk(0, 0);
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        originalChunk.SetPermutation(2, 100, 2, observer);
        original.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        original.SetPermutation(2, 99, 2, stone);

        CompoundTag firstRecord = Assert.IsType<CompoundTag>(
            Assert.Single(originalChunk.PendingTicks!.Get<ListTag>("tickList")!.Value)
        );
        Assert.Equal(1, firstRecord.Get<LongTag>("time")!.Value);

        original.PrepareScheduledTicks(originalChunk);
        Chunk firstSavedChunk = Chunk.Deserialize(0, 0, originalChunk.Serialize(), original);
        using Dimension resumed = new("test:observer-resumed", new VoidGenerator());
        Chunk resumedChunk = resumed.GetOrCreateChunk(0, 0);
        resumedChunk.SetPermutation(2, 100, 2, observer);
        resumedChunk.SetPermutation(2, 99, 2, stone);
        resumedChunk.PendingTicks = firstSavedChunk.PendingTicks;
        resumed.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        resumed.Tick();

        BlockPermutation poweredObserver = resumed.GetPermutation(2, 100, 2);
        Assert.True(poweredObserver.State.Values.GetValueOrDefault("powered_bit") is true);
        CompoundTag releaseRecord = Assert.IsType<CompoundTag>(
            Assert.Single(resumedChunk.PendingTicks!.Get<ListTag>("tickList")!.Value)
        );
        Assert.Equal(3, releaseRecord.Get<LongTag>("time")!.Value);

        resumed.PrepareScheduledTicks(resumedChunk);
        Chunk secondSavedChunk = Chunk.Deserialize(0, 0, resumedChunk.Serialize(), resumed);
        using Dimension restarted = new("test:observer-restarted", new VoidGenerator());
        Chunk restartedChunk = restarted.GetOrCreateChunk(0, 0);
        restartedChunk.SetPermutation(2, 100, 2, poweredObserver);
        restartedChunk.SetPermutation(2, 99, 2, stone);
        restartedChunk.PendingTicks = secondSavedChunk.PendingTicks;
        restarted.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        restarted.Tick();
        Assert.True(restarted.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
        restarted.Tick();
        Assert.False(restarted.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
    }

    [Fact]
    public void ChunkTickClockRemainsSafePastBedrockIntMaximum() {

        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        using Dimension active = new("test:clock-limit-active", new VoidGenerator());
        Chunk activeChunk = active.GetOrCreateChunk(0, 0);
        active.SetPermutation(2, 100, 2, observer);
        CompoundTag scheduled = new();
        scheduled.Add("blockState", BlockStorage.CreatePermutationTag(observer));
        scheduled.Add("time", new LongTag { Value = (long)int.MaxValue + 3 });
        scheduled.Add("x", new IntTag { Value = 2 });
        scheduled.Add("y", new IntTag { Value = 100 });
        scheduled.Add("z", new IntTag { Value = 2 });
        CompoundTag unsupported = new();
        unsupported.Add("blockState", BlockStorage.CreatePermutationTag(
            BlockPalette.FindDefaultPermutation("minecraft:water")!
        ));
        unsupported.Add("time", new LongTag { Value = (long)int.MaxValue + 10 });
        unsupported.Add("x", new IntTag { Value = 3 });
        unsupported.Add("y", new IntTag { Value = 100 });
        unsupported.Add("z", new IntTag { Value = 2 });
        CompoundTag sentinel = new();
        sentinel.Add("blockState", BlockStorage.CreatePermutationTag(observer));
        sentinel.Add("time", new LongTag { Value = -1 });
        sentinel.Add("x", new IntTag { Value = 4 });
        sentinel.Add("y", new IntTag { Value = 100 });
        sentinel.Add("z", new IntTag { Value = 2 });
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = int.MaxValue - 2 });
        pending.Add("tickList", new ListTag {
            ElementType = BasaltBE.Nbt.Enums.TagType.Compound,
            Value = [scheduled, unsupported, sentinel]
        });
        activeChunk.PendingTicks = pending;
        active.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        for (int tick = 0; tick < 4; tick++)
            active.Tick();

        active.PrepareScheduledTicks(activeChunk);
        Assert.Equal(int.MaxValue, pending.Get<IntTag>("currentTick")!.Value);
        Assert.Equal((long)int.MaxValue + 1, scheduled.Get<LongTag>("time")!.Value);
        Assert.Equal((long)int.MaxValue + 8, unsupported.Get<LongTag>("time")!.Value);
        Assert.Equal(-1, sentinel.Get<LongTag>("time")!.Value);

        ChunkStorageEntry[] entries = activeChunk.Serialize();
        active.Tick();
        active.PrepareScheduledTicks(activeChunk);
        Assert.Equal(int.MaxValue, pending.Get<IntTag>("currentTick")!.Value);
        Assert.Equal((long)int.MaxValue + 7, unsupported.Get<LongTag>("time")!.Value);
        Assert.Equal(-1, sentinel.Get<LongTag>("time")!.Value);
        List<Tag> rescheduled = pending.Get<ListTag>("tickList")!.Value;
        Assert.DoesNotContain(rescheduled, tag => ReferenceEquals(tag, scheduled));
        CompoundTag release = Assert.IsType<CompoundTag>(Assert.Single(
            rescheduled,
            tag => tag is CompoundTag item && item.Get<IntTag>("x")?.Value == 2
        ));
        Assert.Equal((long)int.MaxValue + 4, release.Get<LongTag>("time")!.Value);

        using Dimension decoded = new("test:clock-limit-decoded", new VoidGenerator());
        Chunk decodedChunk = Chunk.Deserialize(0, 0, entries, decoded);
        Assert.False(decodedChunk.PendingTicksInvalid);
        Assert.Equal((long)int.MaxValue + 1, Assert.IsType<CompoundTag>(
            Assert.Single(decodedChunk.PendingTicks!.Get<ListTag>("tickList")!.Value,
                record => record is CompoundTag item && item.Get<IntTag>("x")?.Value == 2)
        ).Get<LongTag>("time")!.Value);

        InMemoryProvider provider = new();
        provider.WriteChunks([
            new ChunkStorageRecord(
                new ChunkStorageKey(DimensionType.Overworld, "minecraft:overworld", true, 0, 0),
                entries
            )
        ]);
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using World restartedWorld = new("test:clock-limit-restarted", generators, provider: provider);
        Dimension restarted = restartedWorld.CreateDimension("minecraft:overworld");
        restarted.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        Assert.Equal(ChunkRequestStatus.Queued, restarted.Request(0, 0));
        Stopwatch wait = Stopwatch.StartNew();
        while (restarted.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            restarted.Tick();
            Thread.Sleep(1);
        }

        Chunk restartedChunk = restarted.GetChunk(0, 0)
            ?? throw new Xunit.Sdk.XunitException("Clock-limit chunk did not load from storage.");
        Assert.False(restartedChunk.PendingTicksInvalid);
        Assert.Equal((long)int.MaxValue + 1, Assert.IsType<CompoundTag>(
            Assert.Single(restartedChunk.PendingTicks!.Get<ListTag>("tickList")!.Value,
                record => record is CompoundTag item && item.Get<IntTag>("x")?.Value == 2)
        ).Get<LongTag>("time")!.Value);
        restarted.Tick();
        Assert.True(restarted.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
    }

    [Fact]
    public void ScheduledRedstoneTicksResumeThroughLevelDbWorldRestart() {

        string root = Path.Combine(Path.GetTempPath(), "basalt-scheduled-restart-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "db");
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation button = BlockPalette.FindDefaultPermutation("minecraft:stone_button")!;
        BlockPermutation pressedButton = BlockPalette.Find(button.Identifier, button.State, "button_pressed_bit", true)!;

        try {
            using (World original = new("test", generators, provider: new LevelDbProvider(path))) {
                Dimension originalDimension = original.CreateDimension("minecraft:overworld");
                originalDimension.GetOrCreateChunk(0, 0);
                originalDimension.GetOrCreateChunk(1, 0);
                originalDimension.SetPermutation(2, 100, 2, observer);
                originalDimension.SetPermutation(18, 100, 2, observer);
                originalDimension.SetPermutation(4, 100, 4, pressedButton);
                originalDimension.AddTickingArea("test", [
                    new ChunkPos { X = 0, Z = 0 },
                    new ChunkPos { X = 1, Z = 0 }
                ]);
                originalDimension.SetPermutation(2, 99, 2, stone);
                originalDimension.SetPermutation(18, 99, 2, stone);
                originalDimension.ScheduleBlockUpdate(4, 100, 4, 2);
            }

            using (World resumed = new("test", generators, provider: new LevelDbProvider(path))) {
                Dimension resumedDimension = resumed.CreateDimension("minecraft:overworld");

                resumedDimension.AddTickingArea("active-load", [new ChunkPos { X = 1, Z = 0 }]);
                Assert.Equal(ChunkRequestStatus.Queued, resumedDimension.Request(1, 0));
                Stopwatch activeLoadWait = Stopwatch.StartNew();
                while (resumedDimension.GetChunk(1, 0) is null && activeLoadWait.Elapsed < TimeSpan.FromSeconds(5)) {
                    resumedDimension.Tick();
                    Thread.Sleep(1);
                }

                Chunk activeLoadChunk = resumedDimension.GetChunk(1, 0)
                    ?? throw new Xunit.Sdk.XunitException("Active LevelDB chunk load did not complete.");
                Assert.Single(activeLoadChunk.PendingTicks!.Get<ListTag>("tickList")!.Value);
                Assert.False(resumedDimension.GetPermutation(18, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
                resumedDimension.Tick();
                Assert.True(resumedDimension.GetPermutation(18, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);

                Assert.Equal(ChunkRequestStatus.Queued, resumedDimension.Request(0, 0));
                Stopwatch wait = Stopwatch.StartNew();
                while (resumedDimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                    resumedDimension.Tick();
                    Thread.Sleep(1);
                }

                Chunk restoredChunk = resumedDimension.GetChunk(0, 0)
                    ?? throw new Xunit.Sdk.XunitException("LevelDB chunk load did not complete.");
                Assert.Equal(2, restoredChunk.PendingTicks!.Get<ListTag>("tickList")!.Value.Count);
                Assert.False(resumedDimension.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
                Assert.True(resumedDimension.GetPermutation(4, 100, 4).State.Values.GetValueOrDefault("button_pressed_bit") is true);

                resumedDimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
                resumedDimension.Tick();
                Assert.True(resumedDimension.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
                Assert.True(resumedDimension.GetPermutation(4, 100, 4).State.Values.GetValueOrDefault("button_pressed_bit") is true);
                resumedDimension.Tick();
                Assert.True(resumedDimension.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
                Assert.False(resumedDimension.GetPermutation(4, 100, 4).State.Values.GetValueOrDefault("button_pressed_bit") is true);
                resumedDimension.Tick();
                Assert.False(resumedDimension.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
            }

            using World restarted = new("test", generators, provider: new LevelDbProvider(path));
            Dimension restartedDimension = restarted.CreateDimension("minecraft:overworld");
            Assert.Equal(ChunkRequestStatus.Queued, restartedDimension.Request(0, 0));
            Stopwatch restartWait = Stopwatch.StartNew();
            while (restartedDimension.GetChunk(0, 0) is null && restartWait.Elapsed < TimeSpan.FromSeconds(5)) {
                restartedDimension.Tick();
                Thread.Sleep(1);
            }

            Chunk completedChunk = restartedDimension.GetChunk(0, 0)
                ?? throw new Xunit.Sdk.XunitException("Final LevelDB chunk load did not complete.");
            Assert.Null(completedChunk.PendingTicks);
            Assert.False(restartedDimension.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
            Assert.False(restartedDimension.GetPermutation(4, 100, 4).State.Values.GetValueOrDefault("button_pressed_bit") is true);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PendingChunkReadCannotBeOverwrittenByCreationOrBlockMutation() {
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using Dimension source = new("test:pending-source", new VoidGenerator());
        Chunk sourceChunk = source.GetOrCreateChunk(0, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        sourceChunk.SetPermutation(3, 100, 5, stone);

        using DelayedChunkProvider provider = new(sourceChunk.Serialize());
        using World world = new("test:pending-read", generators, provider: provider);
        Dimension dimension = world.CreateDimension("minecraft:overworld");
        Assert.Equal(ChunkRequestStatus.Queued, dimension.Request(0, 0));
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        InvalidOperationException createError = Assert.Throws<InvalidOperationException>(
            () => dimension.GetOrCreateChunk(0, 0)
        );
        Assert.Contains("still loading", createError.Message);
        Assert.Throws<InvalidOperationException>(
            () => dimension.SetPermutation(3, 100, 5, BlockPalette.FindDefaultPermutation("minecraft:dirt")!)
        );

        provider.ReleaseRead.Set();
        Stopwatch wait = Stopwatch.StartNew();
        while (dimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            dimension.Tick();
            Thread.Sleep(1);
        }

        Assert.Equal(stone, dimension.GetPermutation(3, 100, 5));
        Assert.Equal(0, dimension.PendingRequestCount);
    }

    [Fact]
    public void EqualDeadlineChunkOrderSurvivesQueueCompaction() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Positions = [];
        using Dimension dimension = new("test:stable-order", new VoidGenerator());
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.GetOrCreateChunk(0, 0).SetPermutation(0, 100, 0, stone);
        dimension.GetOrCreateChunk(1, 0).SetPermutation(0, 100, 0, stone);
        dimension.GetOrCreateChunk(2, 0);

        dimension.ScheduleBlockUpdate(0, 100, 0, 100);
        dimension.AddTickingArea("second", [new ChunkPos { X = 1, Z = 0 }]);
        dimension.SetPermutation(16, 100, 0, stone);
        dimension.ScheduleBlockUpdate(16, 100, 0, 100);
        dimension.AddTickingArea("first", [new ChunkPos { X = 0, Z = 0 }]);
        dimension.AddTickingArea("compaction", [new ChunkPos { X = 2, Z = 0 }]);

        for (int index = 0; index < 80; index++) {
            int x = 32 + index % 16;
            int z = index / 16;
            dimension.SetPermutation(x, 100, z, stone);
            dimension.ScheduleBlockUpdate(x, 100, z, (ulong)(90 - index));
        }

        for (int tick = 0; tick < 100; tick++)
            dimension.Tick();

        int[] tiedCallbacks = ScheduledTickProbeTrait.Positions!
            .Where(x => x is 0 or 16)
            .ToArray();
        Assert.Equal([0, 16], tiedCallbacks);
        ScheduledTickProbeTrait.Positions = null;
    }

    [Fact]
    public void EqualDeadlineChunkOrderSurvivesReverseAreaActivation() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Positions = [];
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        ChunkStorageEntry[] firstEntries;
        ChunkStorageEntry[] secondEntries;
        using (Dimension original = new("test:stable-restart-order", new VoidGenerator())) {
            Chunk sourceFirst = original.GetOrCreateChunk(0, 0);
            Chunk sourceSecond = original.GetOrCreateChunk(1, 0);
            original.SetPermutation(1, 100, 1, stone);
            original.SetPermutation(17, 100, 1, stone);
            original.AddTickingArea("both", [new ChunkPos { X = 1, Z = 0 }, new ChunkPos { X = 0, Z = 0 }]);
            original.ScheduleBlockUpdate(1, 100, 1, 10);
            original.ScheduleBlockUpdate(17, 100, 1, 10);
            original.Tick();
            original.Tick();
            original.PrepareScheduledTicks(sourceFirst);
            original.PrepareScheduledTicks(sourceSecond);
            firstEntries = sourceFirst.Serialize();
            secondEntries = sourceSecond.Serialize();
        }

        using Dimension restored = new("test:stable-restart-order", new VoidGenerator());
        Chunk firstRestored = Chunk.Deserialize(0, 0, firstEntries, restored);
        Chunk secondRestored = Chunk.Deserialize(1, 0, secondEntries, restored);
        Chunk first = restored.GetOrCreateChunk(0, 0);
        Chunk second = restored.GetOrCreateChunk(1, 0);
        first.PendingTicks = firstRestored.PendingTicks;
        second.PendingTicks = secondRestored.PendingTicks;
        restored.SetPermutation(1, 100, 1, stone);
        restored.SetPermutation(17, 100, 1, stone);
        restored.AddTickingArea("reverse", [new ChunkPos { X = 1, Z = 0 }, new ChunkPos { X = 0, Z = 0 }]);

        for (int tick = 0; tick < 8; tick++)
            restored.Tick();

        Assert.Equal([1, 17], ScheduledTickProbeTrait.Positions);
        ScheduledTickProbeTrait.Positions = null;
    }

    [Fact]
    public void ReactivatedChunkKeepsItsPlaceInEqualDeadlineRotation() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Executions = 0;
        ScheduledTickProbeTrait.Positions = [];
        using Dimension dimension = new("test:scheduled-area-fairness", new VoidGenerator());
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.GetOrCreateChunk(0, 0);
        dimension.GetOrCreateChunk(1, 0);
        dimension.AddTickingArea("first", [new ChunkPos { X = 0, Z = 0 }]);
        dimension.AddTickingArea("second", [new ChunkPos { X = 1, Z = 0 }]);

        for (int index = 0; index < 150; index++) {
            int x = index & 15;
            int z = index >> 4;
            dimension.SetPermutation(x, 100, z, stone);
            dimension.SetPermutation(16 + x, 100, z, stone);
            dimension.ScheduleBlockUpdate(x, 100, z, 1);
            dimension.ScheduleBlockUpdate(16 + x, 100, z, 1);
        }

        dimension.Tick();
        Assert.Equal(256, ScheduledTickProbeTrait.Executions);
        Assert.True(ScheduledTickProbeTrait.Positions![^1] >= 16);

        ScheduledTickProbeTrait.Positions.Clear();
        dimension.RemoveTickingArea("second");
        dimension.AddTickingArea("second", [new ChunkPos { X = 1, Z = 0 }]);
        dimension.Tick();

        Assert.True(ScheduledTickProbeTrait.Positions![0] < 16);
        Assert.Equal(300, ScheduledTickProbeTrait.Executions);
        ScheduledTickProbeTrait.Positions = null;
    }

    [Fact]
    public void NewerScheduledTickRevisionIsSavedAfterAnOlderWriteCompletes() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using GatedWriteProvider provider = new();
        using World world = new("test:scheduled-save-race", generators, provider: provider);
        Dimension dimension = world.CreateDimension("minecraft:overworld");
        dimension.SetPermutation(0, 100, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        dimension.ScheduleBlockUpdate(0, 100, 0, 30);
        for (int tick = 0; tick < 20; tick++)
            dimension.Tick();
        Assert.True(provider.WriteStarted.Wait(TimeSpan.FromSeconds(5)));

        dimension.ScheduleBlockUpdate(0, 100, 0, 1);
        provider.ReleaseWrite.Set();
        Stopwatch wait = Stopwatch.StartNew();
        while (provider.WriteCount < 2 && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            dimension.Tick();
            Thread.Sleep(1);
        }

        ChunkStorageEntry pending = Assert.Single(provider.LastEntries!, entry => entry.Type == ChunkStorageType.PendingTicks);
        int offset = 0;
        CompoundTag saved = Assert.IsType<CompoundTag>(
            BasaltBE.Nbt.Nbt.Read(pending.Data, ref offset, Options.LittleEndian)
        );
        CompoundTag savedTick = Assert.IsType<CompoundTag>(Assert.Single(saved.Get<ListTag>("tickList")!.Value));
        Assert.Equal(1, savedTick.Get<LongTag>("time")!.Value);
    }

    [Fact]
    public void ShutdownWritesNewerPendingTicksAfterAnOlderWriteInFlight() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        GatedWriteProvider provider = new();
        World world = new("test:shutdown-scheduled-save-race", generators, provider: provider);
        bool disposed = false;

        try {
            Dimension dimension = world.CreateDimension("minecraft:overworld");
            dimension.SetPermutation(0, 100, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
            dimension.ScheduleBlockUpdate(0, 100, 0, 30);
            for (int tick = 0; tick < 20; tick++)
                dimension.Tick();
            Assert.True(provider.WriteStarted.Wait(TimeSpan.FromSeconds(5)));

            dimension.ScheduleBlockUpdate(0, 100, 0, 1);
            provider.ReleaseWrite.Set();
            world.Dispose();
            disposed = true;

            Assert.Equal(2, provider.WriteCount);
            ChunkStorageEntry pending = Assert.Single(provider.LastEntries!, entry => entry.Type == ChunkStorageType.PendingTicks);
            int offset = 0;
            CompoundTag saved = Assert.IsType<CompoundTag>(
                BasaltBE.Nbt.Nbt.Read(pending.Data, ref offset, Options.LittleEndian)
            );
            CompoundTag savedTick = Assert.IsType<CompoundTag>(Assert.Single(saved.Get<ListTag>("tickList")!.Value));
            Assert.Equal(1, savedTick.Get<LongTag>("time")!.Value);

            InMemoryProvider restartedProvider = new();
            restartedProvider.WriteChunks([
                new ChunkStorageRecord(
                    new ChunkStorageKey(DimensionType.Overworld, "minecraft:overworld", true, 0, 0),
                    provider.LastEntries!
                )
            ]);
            using World restarted = new("test:shutdown-scheduled-save-race", generators, provider: restartedProvider);
            Dimension restartedDimension = restarted.CreateDimension("minecraft:overworld");
            Assert.Equal(ChunkRequestStatus.Queued, restartedDimension.Request(0, 0));
            Stopwatch wait = Stopwatch.StartNew();
            while (restartedDimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                restartedDimension.Tick();
                Thread.Sleep(1);
            }

            Chunk restored = restartedDimension.GetChunk(0, 0)
                ?? throw new Xunit.Sdk.XunitException("Shutdown snapshot did not reload.");
            Assert.Equal("minecraft:stone", restartedDimension.GetPermutation(0, 100, 0).Identifier);
            CompoundTag restoredTick = Assert.IsType<CompoundTag>(
                Assert.Single(restored.PendingTicks!.Get<ListTag>("tickList")!.Value)
            );
            Assert.Equal(1, restoredTick.Get<LongTag>("time")!.Value);
        }
        finally {
            if (!disposed) {
                provider.ReleaseWrite.Set();
                world.Dispose();
            }
        }
    }

    [Fact]
    public void FailedChunkWriteRetriesWithoutDroppingPendingTicks() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using FailingWriteProvider provider = new();
        using World world = new("test:write-retry", generators, provider: provider);
        Dimension dimension = world.CreateDimension("minecraft:overworld");
        dimension.SetPermutation(0, 100, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        dimension.ScheduleBlockUpdate(0, 100, 0, 30);
        for (int tick = 0; tick < 20; tick++)
            dimension.Tick();
        Assert.True(provider.FirstWriteStarted.Wait(TimeSpan.FromSeconds(5)));

        Stopwatch wait = Stopwatch.StartNew();
        while (provider.WriteCount < 2 && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            dimension.Tick();
            Thread.Sleep(1);
        }

        Assert.True(provider.WriteCount >= 2);
        ChunkStorageEntry pending = Assert.Single(provider.LastEntries!, entry => entry.Type == ChunkStorageType.PendingTicks);
        int offset = 0;
        CompoundTag saved = Assert.IsType<CompoundTag>(
            BasaltBE.Nbt.Nbt.Read(pending.Data, ref offset, Options.LittleEndian)
        );
        CompoundTag savedTick = Assert.IsType<CompoundTag>(Assert.Single(saved.Get<ListTag>("tickList")!.Value));
        Assert.Equal(30, savedTick.Get<LongTag>("time")!.Value);
    }

    [Fact]
    public void DirtyChunkRetriesAfterStorageQueueBackpressure() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using GatedWriteProvider provider = new();
        using World world = new("test:storage-backpressure", generators, provider: provider);

        try {
            Dimension dimension = world.CreateDimension("minecraft:overworld");
            BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
            dimension.SetPermutation(0, 100, 0, stone);
            dimension.ScheduleBlockUpdate(0, 100, 0, 30);
            for (int tick = 0; tick < 20; tick++)
                dimension.Tick();

            Assert.True(provider.WriteStarted.Wait(TimeSpan.FromSeconds(5)));
            WorldProviderService storage = world.Storage!;
            ChunkStorageEntry[] filler = [new(ChunkStorageType.PendingTicks, 0, [1])];
            for (int index = 0; index < 4095; index++) {
                ChunkStorageKey key = new(DimensionType.Overworld, dimension.Identifier, true, 1000 + index, 0);
                Assert.True(storage.Write(dimension, key, index, filler));
            }

            dimension.SetPermutation(17, 100, 1, stone);
            dimension.ScheduleBlockUpdate(17, 100, 1, 30);
            for (int tick = 0; tick < 20; tick++)
                dimension.Tick();

            Assert.Equal(4096, storage.PendingOperations);
            provider.ReleaseWrite.Set();
            Assert.True(SpinWait.SpinUntil(() => storage.PendingOperations == 0, TimeSpan.FromSeconds(10)));

            for (int tick = 0; tick < 20; tick++)
                dimension.Tick();

            Stopwatch wait = Stopwatch.StartNew();
            bool savedSecondChunk = false;
            ChunkStorageKey secondChunkKey = new(DimensionType.Overworld, dimension.Identifier, true, 1, 0);
            while (!savedSecondChunk && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                dimension.Tick();
                if (provider.Chunks.TryGetValue(secondChunkKey, out ChunkStorageEntry[]? entries) &&
                    entries.SingleOrDefault(entry => entry.Type == ChunkStorageType.PendingTicks) is { Data.Length: > 0 } pendingEntry) {
                    int offset = 0;
                    if (BasaltBE.Nbt.Nbt.Read(pendingEntry.Data, ref offset, Options.LittleEndian) is CompoundTag pending &&
                        pending.Get<ListTag>("tickList") is { } ticks) {
                        savedSecondChunk = ticks.Value.Any(tag =>
                            tag is CompoundTag record && record.Get<IntTag>("x")?.Value == 17);
                    }
                }
                Thread.Sleep(1);
            }

            Assert.True(savedSecondChunk);
            Assert.True(SpinWait.SpinUntil(() => storage.PendingOperations == 0, TimeSpan.FromSeconds(5)));
        }
        finally {
            provider.ReleaseWrite.Set();
        }
    }

    [Fact]
    public void FailedChunkReadClearsPendingRequestWithoutGeneratingReplacementTerrain() {
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        using FailingReadProvider provider = new();
        using World world = new("test:read-failure", generators, provider: provider);
        Dimension dimension = world.CreateDimension("minecraft:overworld");
        Assert.Equal(ChunkRequestStatus.Queued, dimension.Request(0, 0));
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        Stopwatch wait = Stopwatch.StartNew();
        while (dimension.PendingRequestCount != 0 && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            dimension.Tick();
            Thread.Sleep(1);
        }

        Assert.Equal(0, dimension.PendingRequestCount);
        Assert.Null(dimension.GetChunk(0, 0));
    }

    [Fact]
    public void CallbackCanRescheduleItselfAndAnExceptionDoesNotCorruptTheScheduler() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Executions = 0;
        ScheduledTickProbeTrait.Positions = [];
        ScheduledTickProbeTrait.Callback = (dimension, x, y, z) => {
            if (Interlocked.Increment(ref ScheduledTickProbeTrait.CallbackRuns) == 1)
                dimension.ScheduleBlockUpdate(x, y, z, 1);
            else
                throw new InvalidOperationException("Expected test failure.");
        };

        try {
            using Dimension dimension = new("test:callback-reschedule", new VoidGenerator());
            Chunk chunk = dimension.GetOrCreateChunk(0, 0);
            dimension.SetPermutation(1, 100, 1, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
            dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
            dimension.ScheduleBlockUpdate(1, 100, 1, 1);
            dimension.Tick();

            Assert.Single(chunk.PendingTicks!.Get<ListTag>("tickList")!.Value);
            dimension.Tick();

            Assert.Equal(2, ScheduledTickProbeTrait.Executions);
            Assert.Equal([1, 1], ScheduledTickProbeTrait.Positions);
            Assert.Null(chunk.PendingTicks);
        }
        finally {
            ScheduledTickProbeTrait.Callback = null;
            ScheduledTickProbeTrait.CallbackRuns = 0;
            ScheduledTickProbeTrait.Positions = null;
        }
    }

    [Fact]
    public void ValidPendingTicksRunAlongsideMalformedRecordsWhichRemainUnchanged() {

        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        InMemoryProvider provider = new();
        using Dimension source = new("test:malformed-pending-ticks-source", new VoidGenerator());
        Chunk sourceChunk = source.GetOrCreateChunk(0, 0);
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        sourceChunk.SetPermutation(2, 100, 2, observer);

        CompoundTag malformed = new();
        malformed.Add("blockState", BlockStorage.CreatePermutationTag(observer));
        malformed.Add("x", new IntTag { Value = 2 });
        malformed.Add("y", new IntTag { Value = 100 });
        malformed.Add("z", new IntTag { Value = 2 });
        CompoundTag unsupported = new();
        CompoundTag waterState = BlockStorage.CreatePermutationTag(
            BlockPalette.FindDefaultPermutation("minecraft:water")!
        );
        unsupported.Add("blockState", waterState);
        unsupported.Add("time", new LongTag { Value = 15 });
        unsupported.Add("x", new IntTag { Value = 3 });
        unsupported.Add("y", new IntTag { Value = 100 });
        unsupported.Add("z", new IntTag { Value = 2 });
        CompoundTag sentinel = new();
        sentinel.Add("blockState", BlockStorage.CreatePermutationTag(observer));
        sentinel.Add("time", new LongTag { Value = -1 });
        sentinel.Add("x", new IntTag { Value = 4 });
        sentinel.Add("y", new IntTag { Value = 100 });
        sentinel.Add("z", new IntTag { Value = 2 });
        CompoundTag scheduled = new();
        scheduled.Add("blockState", BlockStorage.CreatePermutationTag(observer));
        scheduled.Add("time", new LongTag { Value = 13 });
        scheduled.Add("x", new IntTag { Value = 2 });
        scheduled.Add("y", new IntTag { Value = 100 });
        scheduled.Add("z", new IntTag { Value = 2 });
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 12 });
        pending.Add("tickList", new ListTag {
            ElementType = BasaltBE.Nbt.Enums.TagType.Compound,
            Value = [malformed, unsupported, sentinel, scheduled]
        });
        sourceChunk.PendingTicks = pending;
        byte[] malformedBefore = new byte[malformed.GetSize(Options.LittleEndian)];
        int malformedOffset = 0;
        BasaltBE.Nbt.Nbt.Write(malformed, malformedBefore, ref malformedOffset, Options.LittleEndian);
        provider.WriteChunks([
            new ChunkStorageRecord(
                new ChunkStorageKey(DimensionType.Overworld, "minecraft:overworld", true, 0, 0),
                sourceChunk.Serialize()
            )
        ]);

        using World world = new("test:malformed-pending-ticks", generators, provider: provider);
        Dimension loaded = world.CreateDimension("minecraft:overworld");
        loaded.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        Assert.Equal(ChunkRequestStatus.Queued, loaded.Request(0, 0));
        Stopwatch wait = Stopwatch.StartNew();
        while (loaded.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
            loaded.Tick();
            Thread.Sleep(1);
        }

        Chunk chunk = loaded.GetChunk(0, 0)
            ?? throw new Xunit.Sdk.XunitException("Pending tick chunk did not load from storage.");
        loaded.Tick();

        Assert.False(chunk.PendingTicksInvalid);
        Assert.True(loaded.GetPermutation(2, 100, 2).State.Values.GetValueOrDefault("powered_bit") is true);
        List<Tag> remaining = chunk.PendingTicks!.Get<ListTag>("tickList")!.Value;
        CompoundTag preserved = Assert.IsType<CompoundTag>(
            Assert.Single(remaining, record => record is CompoundTag item && item.Get<LongTag>("time") is null)
        );
        byte[] malformedAfter = new byte[preserved.GetSize(Options.LittleEndian)];
        int preservedOffset = 0;
        BasaltBE.Nbt.Nbt.Write(preserved, malformedAfter, ref preservedOffset, Options.LittleEndian);
        Assert.Equal(malformedBefore, malformedAfter);
        Assert.Equal(2, remaining.Count(record => record is CompoundTag item && item.Get<LongTag>("time")?.Value == 15));
        Assert.Contains(remaining, record => record is CompoundTag item && item.Get<LongTag>("time")?.Value == -1);
    }

    [Fact]
    public void DueUpdateBudgetSpreadsLargeBatchesAcrossActiveChunks() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Executions = 0;
        ScheduledTickProbeTrait.Positions = [];
        using Dimension dimension = new("test:due-update-budget", new VoidGenerator());
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.GetOrCreateChunk(0, 0);
        dimension.GetOrCreateChunk(1, 0);
        dimension.AddTickingArea("first", [new ChunkPos { X = 0, Z = 0 }]);
        dimension.AddTickingArea("second", [new ChunkPos { X = 1, Z = 0 }]);

        for (int index = 0; index < 150; index++) {
            int x = index & 15;
            int z = index >> 4;
            dimension.SetPermutation(x, 100, z, stone);
            dimension.SetPermutation(16 + x, 100, z, stone);
            dimension.ScheduleBlockUpdate(x, 100, z, 1);
            dimension.ScheduleBlockUpdate(16 + x, 100, z, 1);
        }

        dimension.Tick();

        Assert.Equal(256, ScheduledTickProbeTrait.Executions);
        Assert.Contains(ScheduledTickProbeTrait.Positions!, x => x < 16);
        Assert.Contains(ScheduledTickProbeTrait.Positions!, x => x >= 16);
        Assert.Equal(2, dimension.OverdueScheduledChunks);

        dimension.Tick();

        Assert.Equal(300, ScheduledTickProbeTrait.Executions);
        Assert.Equal(0, dimension.OverdueScheduledChunks);
        Assert.Equal(0UL, dimension.OldestScheduledTickOverdue);
        ScheduledTickProbeTrait.Positions = null;
    }

    [Fact]
    public void BenchmarkScheduledTicksAcrossManyActiveChunks() {
        if (Interlocked.Exchange(ref manyChunkBenchmarkRegistered, 1) == 0)
            BlockPalette.RegisterTrait<ManyChunkScheduledTickTrait>();

        const int chunkCount = 64;
        const int hotChunkCount = 4;
        const int hotUpdatesPerChunk = 512;
        const int coldUpdatesPerChunk = 32;
        const int totalUpdates = hotChunkCount * hotUpdatesPerChunk +
            (chunkCount - hotChunkCount) * coldUpdatesPerChunk;
        const int warmupCount = 4;
        const int sampleCount = 20;
        const int maximumDrainTicks = totalUpdates / 256 + 1;
        ChunkPos[] area = new ChunkPos[chunkCount];
        for (int chunk = 0; chunk < chunkCount; chunk++)
            area[chunk] = new ChunkPos { X = chunk, Z = 0 };

        double[] scheduleSamples = new double[sampleCount];
        double[] drainSamples = new double[sampleCount];
        int[] drainTickSamples = new int[sampleCount];
        List<double> tickSamples = new(sampleCount * maximumDrainTicks);
        List<double> ticksWithoutGc = new(sampleCount * maximumDrainTicks);
        int ticksWithGc = 0;
        long scheduledBytes = 0;
        long dispatchBytes = 0;
        for (int sample = -warmupCount; sample < sampleCount; sample++) {
            using Dimension dimension = new("test:many-chunk-scheduled-ticks", new VoidGenerator());
            ManyChunkScheduledTickTrait.Executions = 0;
            ManyChunkScheduledTickTrait.ChunkExecutions = new int[maximumDrainTicks, chunkCount];
            dimension.AddTickingArea("benchmark", area);
            BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;

            for (int chunk = 0; chunk < chunkCount; chunk++) {
                int updates = chunk < hotChunkCount ? hotUpdatesPerChunk : coldUpdatesPerChunk;
                for (int index = 0; index < updates; index++) {
                    int x = chunk * 16 + (index & 15);
                    int z = index >> 4 & 15;
                    int y = 100 + (index >> 8);
                    dimension.SetPermutation(x, y, z, stone);
                }
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long scheduleStart = Stopwatch.GetTimestamp();
            for (int chunk = 0; chunk < chunkCount; chunk++) {
                int updates = chunk < hotChunkCount ? hotUpdatesPerChunk : coldUpdatesPerChunk;
                for (int index = 0; index < updates; index++) {
                    int x = chunk * 16 + (index & 15);
                    int z = index >> 4 & 15;
                    int y = 100 + (index >> 8);
                    dimension.ScheduleBlockUpdate(x, y, z, 1);
                }
            }
            long scheduleEnd = Stopwatch.GetTimestamp();
            long scheduleAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            long drainStart = Stopwatch.GetTimestamp();
            long tickAllocated = 0;
            int tick = 0;
            while (ManyChunkScheduledTickTrait.Executions < totalUpdates) {
                ManyChunkScheduledTickTrait.CurrentTick = tick;
                long tickAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);
                long tickStart = Stopwatch.GetTimestamp();
                dimension.Tick();
                long tickEnd = Stopwatch.GetTimestamp();
                if (sample >= 0) {
                    double elapsed = Stopwatch.GetElapsedTime(tickStart, tickEnd).TotalMilliseconds;
                    tickSamples.Add(elapsed);
                    if (gen0Before == GC.CollectionCount(0) && gen1Before == GC.CollectionCount(1) &&
                        gen2Before == GC.CollectionCount(2))
                        ticksWithoutGc.Add(elapsed);
                    else
                        ticksWithGc++;
                    tickAllocated += GC.GetAllocatedBytesForCurrentThread() - tickAllocatedBefore;
                }
                tick++;
            }
            long drainEnd = Stopwatch.GetTimestamp();
            long dispatchAllocated = tickAllocated;

            Assert.Equal(totalUpdates, ManyChunkScheduledTickTrait.Executions);
            Assert.Equal(maximumDrainTicks, tick);
            for (int activeTick = 0; activeTick < maximumDrainTicks; activeTick++) {
                for (int chunk = 0; chunk < chunkCount; chunk++) {
                    int expectedCallbacks = 0;
                    if (activeTick < 8)
                        expectedCallbacks = 4;
                    else if (chunk < hotChunkCount)
                        expectedCallbacks = activeTick < maximumDrainTicks - 1 ? 64 : 32;
                    Assert.Equal(expectedCallbacks, ManyChunkScheduledTickTrait.ChunkExecutions[activeTick, chunk]);
                }
            }

            if (sample >= 0) {
                scheduleSamples[sample] = Stopwatch.GetElapsedTime(scheduleStart, scheduleEnd).TotalMilliseconds;
                drainSamples[sample] = Stopwatch.GetElapsedTime(drainStart, drainEnd).TotalMilliseconds;
                drainTickSamples[sample] = tick;
                scheduledBytes += scheduleAllocated;
                dispatchBytes += dispatchAllocated;
            }
        }

        Array.Sort(scheduleSamples);
        Array.Sort(drainSamples);
        Array.Sort(drainTickSamples);
        tickSamples.Sort();
        ticksWithoutGc.Sort();
        double tickMedian = (tickSamples[tickSamples.Count / 2 - 1] + tickSamples[tickSamples.Count / 2]) / 2;
        int tickP95 = (int)Math.Ceiling(tickSamples.Count * 0.95) - 1;
        int tickP99 = (int)Math.Ceiling(tickSamples.Count * 0.99) - 1;
        int noGcP95 = (int)Math.Ceiling(ticksWithoutGc.Count * 0.95) - 1;
        int noGcP99 = (int)Math.Ceiling(ticksWithoutGc.Count * 0.99) - 1;
        output.WriteLine(
            $"Synthetic scheduler callbacks across {chunkCount} active chunks: {totalUpdates} equal-deadline events " +
            $"(4 chunks x {hotUpdatesPerChunk}, 60 chunks x {coldUpdatesPerChunk}), 256 per tick, " +
            $"{warmupCount} warmups, {sampleCount} samples, no viewers/provider. " +
            $"Schedule median/p95/p99/max: {(scheduleSamples[9] + scheduleSamples[10]) / 2:F3}/" +
            $"{scheduleSamples[18]:F3}/{scheduleSamples[19]:F3}/{scheduleSamples[19]:F3} ms; " +
            $"drain median/p95/p99/max: {(drainSamples[9] + drainSamples[10]) / 2:F3}/" +
            $"{drainSamples[18]:F3}/{drainSamples[19]:F3}/{drainSamples[19]:F3} ms; " +
            $"tick median/p95/p99/max: {tickMedian:F3}/{tickSamples[tickP95]:F3}/" +
            $"{tickSamples[tickP99]:F3}/{tickSamples[^1]:F3} ms; " +
            $"ticks with GC: {ticksWithGc}/{tickSamples.Count}, no-GC p95/p99/max: " +
            $"{ticksWithoutGc[noGcP95]:F3}/{ticksWithoutGc[noGcP99]:F3}/{ticksWithoutGc[^1]:F3} ms; " +
            $"drain ticks median/max: {drainTickSamples[9]}/{drainTickSamples[^1]}; " +
            $"allocation per sample schedule/dispatch: " +
            $"{scheduledBytes / (double)sampleCount / 1024:F1}/{dispatchBytes / (double)sampleCount / 1024:F1} KiB " +
            $"({dispatchBytes / (double)sampleCount / totalUpdates:F1} bytes per callback)."
        );
    }

    [Fact]
    public void BenchmarkObserverPulsesAcrossManyActiveChunks() {

        const int chunkCount = 64;
        const int observersPerChunk = 32;
        const int totalObservers = chunkCount * observersPerChunk;
        const int totalCallbacks = totalObservers * 2;
        const int warmupCount = 4;
        const int sampleCount = 20;
        const int maximumDrainTicks = totalCallbacks / 256 + 4;
        ChunkPos[] area = new ChunkPos[chunkCount];
        for (int chunk = 0; chunk < chunkCount; chunk++)
            area[chunk] = new ChunkPos { X = chunk, Z = 0 };

        double[] scheduleSamples = new double[sampleCount];
        double[] drainSamples = new double[sampleCount];
        int[] drainTickSamples = new int[sampleCount];
        List<double> tickSamples = new(sampleCount * maximumDrainTicks);
        long scheduledBytes = 0;
        long dispatchBytes = 0;
        for (int sample = -warmupCount; sample < sampleCount; sample++) {
            using Dimension dimension = new("test:many-chunk-observer-pulses", new VoidGenerator());
            BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
            for (int chunk = 0; chunk < chunkCount; chunk++) {
                for (int index = 0; index < observersPerChunk; index++) {
                    int x = chunk * 16 + (index & 15);
                    int z = index >> 4;
                    dimension.SetPermutation(x, 100, z, observer);
                }
            }
            dimension.AddTickingArea("benchmark", area);
            bool[] previousPower = new bool[totalObservers];
            int[] observerTransitions = new int[totalObservers];
            for (int chunk = 0; chunk < chunkCount; chunk++) {
                for (int index = 0; index < observersPerChunk; index++) {
                    int x = chunk * 16 + (index & 15);
                    int z = index >> 4;
                    int observerIndex = chunk * observersPerChunk + index;
                    previousPower[observerIndex] =
                        dimension.GetPermutation(x, 100, z).State.Values.GetValueOrDefault("powered_bit") is true;
                }
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long scheduleStart = Stopwatch.GetTimestamp();
            for (int chunk = 0; chunk < chunkCount; chunk++) {
                for (int index = 0; index < observersPerChunk; index++) {
                    int x = chunk * 16 + (index & 15);
                    int z = index >> 4;
                    dimension.ScheduleBlockUpdate(x, 100, z, 1);
                }
            }
            long scheduleEnd = Stopwatch.GetTimestamp();
            long scheduleAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            int stateTransitions = 0;
            int tick = 0;
            long drainStart = Stopwatch.GetTimestamp();
            long tickAllocated = 0;
            bool pending;
            do {
                long tickAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long tickStart = Stopwatch.GetTimestamp();
                dimension.Tick();
                long tickEnd = Stopwatch.GetTimestamp();
                if (sample >= 0) {
                    tickSamples.Add(Stopwatch.GetElapsedTime(tickStart, tickEnd).TotalMilliseconds);
                    tickAllocated += GC.GetAllocatedBytesForCurrentThread() - tickAllocatedBefore;
                }

                for (int chunk = 0; chunk < chunkCount; chunk++) {
                    for (int index = 0; index < observersPerChunk; index++) {
                        int x = chunk * 16 + (index & 15);
                        int z = index >> 4;
                        int observerIndex = chunk * observersPerChunk + index;
                        bool powered = dimension.GetPermutation(x, 100, z).State.Values.GetValueOrDefault("powered_bit") is true;
                        if (powered != previousPower[observerIndex]) {
                            previousPower[observerIndex] = powered;
                            observerTransitions[observerIndex]++;
                            stateTransitions++;
                        }
                    }
                }

                pending = false;
                for (int chunk = 0; chunk < chunkCount; chunk++)
                    pending |= dimension.GetChunk(chunk, 0)!.PendingTicks is not null;

                tick++;
            } while (pending && tick < maximumDrainTicks);
            long drainEnd = Stopwatch.GetTimestamp();

            Assert.False(pending);
            Assert.InRange(tick, 1, maximumDrainTicks);
            Assert.Equal(totalCallbacks, stateTransitions);
            Assert.All(observerTransitions, transitions => Assert.Equal(2, transitions));
            if (sample >= 0) {
                scheduleSamples[sample] = Stopwatch.GetElapsedTime(scheduleStart, scheduleEnd).TotalMilliseconds;
                drainSamples[sample] = Stopwatch.GetElapsedTime(drainStart, drainEnd).TotalMilliseconds;
                drainTickSamples[sample] = tick;
                scheduledBytes += scheduleAllocated;
                dispatchBytes += tickAllocated;
            }
        }

        Array.Sort(scheduleSamples);
        Array.Sort(drainSamples);
        Array.Sort(drainTickSamples);
        tickSamples.Sort();
        double tickMedian = (tickSamples[tickSamples.Count / 2 - 1] + tickSamples[tickSamples.Count / 2]) / 2;
        int tickP95 = (int)Math.Ceiling(tickSamples.Count * 0.95) - 1;
        int tickP99 = (int)Math.Ceiling(tickSamples.Count * 0.99) - 1;
        output.WriteLine(
            $"Observer pulses across {chunkCount} active chunks: {totalObservers} observers, {totalCallbacks} real scheduled callbacks, " +
            $"{warmupCount} warmups, {sampleCount} samples, no viewers/provider. " +
            $"Schedule median/p95/p99/max: {(scheduleSamples[9] + scheduleSamples[10]) / 2:F3}/" +
            $"{scheduleSamples[18]:F3}/{scheduleSamples[19]:F3}/{scheduleSamples[19]:F3} ms; " +
            $"drain median/p95/p99/max: {(drainSamples[9] + drainSamples[10]) / 2:F3}/" +
            $"{drainSamples[18]:F3}/{drainSamples[19]:F3}/{drainSamples[19]:F3} ms; " +
            $"tick median/p95/p99/max: {tickMedian:F3}/{tickSamples[tickP95]:F3}/" +
            $"{tickSamples[tickP99]:F3}/{tickSamples[^1]:F3} ms; " +
            $"drain ticks median/max: {drainTickSamples[9]}/{drainTickSamples[^1]}; " +
            $"allocation per sample schedule/dispatch: " +
            $"{scheduledBytes / (double)sampleCount / 1024:F1}/{dispatchBytes / (double)sampleCount / 1024:F1} KiB."
        );
    }

    [Fact]
    public async Task BenchmarkServerTickLoadWithSyntheticPlayersAndScheduledRedstone() {

        using TcpListener reservation = new(IPAddress.Loopback, 0);
        reservation.Start();
        ushort port = (ushort)((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();

        string root = Path.Combine(Path.GetTempPath(), "basalt-scheduled-tick-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Server server = new(new ServerProperties {
            DefaultWorldName = "load",
            ServerPort = port,
            DiscoveryPort = 0,
            MaxViewDistance = 4,
            Ticking = "default",
            AutosaveIntervalSeconds = 5
        }, new BenchmarkIdentity());
        server.Generators.Register<VoidGenerator>("void");
        server.Generators.SetDefault("void");
        World world = server.CreateWorld("load", value => value.CreateDimension("minecraft:overworld"),
            new LevelDbProvider(Path.Combine(root, "db")));
        Dimension dimension = world.GetDimension("minecraft:overworld")!;

        const int playerCount = 20;
        const int baselineTicks = 20;
        const int warmupTicks = 20;
        const int measuredTicks = 120;
        const int observerCount = 32;
        const float playerStep = 0.8f;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        using CancellationTokenSource receiveStop = new();
        List<NethernetClient> clients = new(playerCount);
        List<Player> players = new(playerCount);
        List<Task> readers = new(playerCount);
        long receivedMessages = 0;
        long receivedBytes = 0;
        long allocations = 0;
        List<double> baselineSamples = new(baselineTicks);
        List<double> serverTickSamples = new(measuredTicks);
        List<double> workloadSamples = new(measuredTicks);
        long maximumOutgoingBytes = 0;
        int observerTransitions = 0;

        try {
            server.Network.Start();
            for (int tick = 0; tick < baselineTicks; tick++) {
                long start = Stopwatch.GetTimestamp();
                server.Tick();
                baselineSamples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                Thread.Sleep(50);
            }

            for (int index = 0; index < playerCount; index++) {
                NethernetClient client = new(new NethernetClientOptions {
                    SignalingUri = new Uri($"http://127.0.0.1:{port}")
                });
                clients.Add(client);
                await client.ConnectAsync(timeout.Token);
            }

            FieldInfo connectionField = typeof(NethernetClient).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic)!;
            FieldInfo transportField = typeof(BasaltBE.Basalt.Network.NetworkServer)
                .GetField("nethernet", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object transport = transportField.GetValue(server.Network)!;
            ConcurrentDictionary<NethernetPeer, NethernetConnection> peers = (ConcurrentDictionary<NethernetPeer, NethernetConnection>)
                transport.GetType().GetField("connections", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(transport)!;
            Assert.True(SpinWait.SpinUntil(() => peers.Count == playerCount, TimeSpan.FromSeconds(10)));
            foreach (NethernetClient client in clients) {
                Task reader = Task.Run(async () => {
                    try {
                        while (!receiveStop.IsCancellationRequested) {
                            NethernetMessage message = await client.ReceiveAsync(receiveStop.Token);
                            if (message.Disconnected)
                                break;
                            Interlocked.Increment(ref receivedMessages);
                            Interlocked.Add(ref receivedBytes, message.Payload.Length);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                });
                readers.Add(reader);

                NethernetConnection connection = (NethernetConnection)connectionField.GetValue(client)!;
                NethernetPeer peer = peers.Keys.First(candidate => candidate.NetworkId == connection.Peer.NetworkId);
                Player player = new(server.Network, world, dimension, peer,
                    $"Load{players.Count}", (players.Count + 1).ToString(), Guid.NewGuid(), 8, "benchmark", []) {
                    ChunkRadius = 1,
                    Spawned = true,
                    Listed = false
                };
                server.Network.AddPlayer(player);
                player.Position.X = players.Count * 48;
                player.OnMove();
                player.OnSpawn();
                players.Add(player);
            }

            for (int tick = 0; tick < warmupTicks; tick++) {
                server.Tick();
                Thread.Sleep(50);
            }

            Assert.NotNull(dimension.GetChunk(0, 0));
            Assert.Equal(playerCount, dimension.GetPlayers().Length);
            BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
            for (int index = 0; index < observerCount; index++)
                dimension.SetPermutation(index & 15, 100, index >> 4, observer);
            bool[] observerPowered = new bool[observerCount];

            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < measuredTicks; tick++) {
                long workloadStart = Stopwatch.GetTimestamp();
                if (tick % 8 == 0) {
                    for (int index = 0; index < observerCount; index++)
                        dimension.ScheduleBlockUpdate(index & 15, 100, index >> 4, 1);
                }

                for (int index = 0; index < players.Count; index++) {
                    Player player = players[index];
                    if (tick == measuredTicks / 2) {
                        player.Position.X = index % 4 * 0.8f;
                        player.Position.Z = index / 4 * 0.8f;
                    }
                    else {
                        player.Position.X += playerStep;
                    }
                    player.OnMove();
                }

                long start = Stopwatch.GetTimestamp();
                server.Tick();
                serverTickSamples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                workloadSamples.Add(Stopwatch.GetElapsedTime(workloadStart).TotalMilliseconds);
                for (int index = 0; index < observerCount; index++) {
                    bool powered = dimension.GetPermutation(index & 15, 100, index >> 4)
                        .State.Values.GetValueOrDefault("powered_bit") is true;
                    if (powered != observerPowered[index]) {
                        observerPowered[index] = powered;
                        observerTransitions++;
                    }
                }
                foreach (Player player in players)
                    maximumOutgoingBytes = Math.Max(maximumOutgoingBytes, (long)server.Network.GetOutgoingBufferedBytes(player));
                Thread.Sleep(50);
            }
            allocations = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.True(observerTransitions > 0);
            Assert.True(Interlocked.Read(ref receivedMessages) > 0);

            Stopwatch saveTime = Stopwatch.StartNew();
            bool saved = server.SaveAll();
            Assert.True(saved);
            Assert.True(SpinWait.SpinUntil(() => world.StorageMetrics.PendingOperations == 0, TimeSpan.FromSeconds(30)));
            saveTime.Stop();

            baselineSamples.Sort();
            serverTickSamples.Sort();
            workloadSamples.Sort();
            output.WriteLine(
                $"Server tick load, synthetic-auth local transport: players {playerCount}, separated then clustered, " +
                $"baseline {baselineTicks} ticks, " +
                $"warmup {warmupTicks}, measured {measuredTicks}, autosave 5 s, provider leveldb, render radius 1. " +
                $"Baseline tick median/p95/max: {(baselineSamples[9] + baselineSamples[10]) / 2:F3}/" +
                $"{baselineSamples[(int)Math.Ceiling(baselineSamples.Count * 0.95) - 1]:F3}/{baselineSamples[^1]:F3} ms. " +
                $"Server.Tick median/p95/p99/max: {(serverTickSamples[59] + serverTickSamples[60]) / 2:F3}/" +
                $"{serverTickSamples[(int)Math.Ceiling(serverTickSamples.Count * 0.95) - 1]:F3}/" +
                $"{serverTickSamples[(int)Math.Ceiling(serverTickSamples.Count * 0.99) - 1]:F3}/{serverTickSamples[^1]:F3} ms; " +
                $"driver + tick median/p95/p99/max: {(workloadSamples[59] + workloadSamples[60]) / 2:F3}/" +
                $"{workloadSamples[(int)Math.Ceiling(workloadSamples.Count * 0.95) - 1]:F3}/" +
                $"{workloadSamples[(int)Math.Ceiling(workloadSamples.Count * 0.99) - 1]:F3}/{workloadSamples[^1]:F3} ms; " +
                $"chunks: {dimension.ChunkCount}, observer schedule requests: {observerCount * ((measuredTicks + 7) / 8)}, " +
                $"observer state transitions: {observerTransitions}, received transport messages/bytes: " +
                $"{receivedMessages}/{receivedBytes}, " +
                $"max outgoing backlog: {maximumOutgoingBytes} bytes, tick-thread allocation: {allocations / 1024.0:F1} KiB, " +
                $"GC collections Gen0/1/2: {GC.CollectionCount(0) - gen0Before}/" +
                $"{GC.CollectionCount(1) - gen1Before}/{GC.CollectionCount(2) - gen2Before}, " +
                $"storage save/flush wait: {saveTime.Elapsed.TotalMilliseconds:F3} ms, " +
                $"storage writes/average write: {world.StorageMetrics.WriteCount}/" +
                $"{world.StorageMetrics.AverageWriteMilliseconds:F3} ms per chunk, " +
                $"pending operations/bytes: {world.StorageMetrics.PendingOperations}/{world.StorageMetrics.PendingBytes}. " +
                "The driver applies player movement directly and calls Entity.OnMove. Workload time includes this driver work; " +
                "Server.Tick excludes it. Both bypass Bedrock auth input and retail client rendering."
            );
        }
        finally {
            receiveStop.Cancel();
            try {
                foreach (NethernetClient client in clients)
                    await client.DisposeAsync();
                await Task.WhenAll(readers);
            }
            finally {
                try {
                    server.Dispose();
                }
                finally {
                    Directory.Delete(root, recursive: true);
                }
            }
        }
    }

    [Fact]
    public void RepeaterAndTorchTransitionsResumeThroughLevelDbWorldRestart() {
        if (Interlocked.Exchange(ref redstoneDevicesRegistered, 1) == 0) {
            BlockPalette.RegisterTrait<RedstoneDeviceTrait>();
            BlockPalette.RegisterTrait<RedstoneTorchTrait>();
        }
        if (Interlocked.Exchange(ref daylightDetectorRegistered, 1) == 0)
            BlockPalette.RegisterTrait<DaylightDetectorTrait>();

        string root = Path.Combine(Path.GetTempPath(), "basalt-redstone-device-restart-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "db");
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        BlockPermutation repeater = BlockPalette.FindDefaultPermutation("minecraft:unpowered_repeater")!;
        repeater = BlockPalette.Find(repeater.Identifier, repeater.State, "minecraft:cardinal_direction", "north")!;
        BlockPermutation torch = BlockPalette.FindDefaultPermutation("minecraft:redstone_torch")!;
        torch = BlockPalette.Find(torch.Identifier, torch.State, "torch_facing_direction", "top")!;
        BlockPermutation comparator = BlockPalette.FindDefaultPermutation("minecraft:unpowered_comparator")!;
        comparator = BlockPalette.Find(comparator.Identifier, comparator.State, "minecraft:cardinal_direction", "north")!;
        BlockPermutation daylightDetector = BlockPalette.FindDefaultPermutation("minecraft:daylight_detector")!;

        try {
            using (Server original = new(new ServerProperties { DefaultWorldName = "test" })) {
                original.BlockActors.Register("minecraft:powered_comparator", static context => new ComparatorBlockActor(context));
                original.BlockActors.Register("minecraft:unpowered_comparator", static context => new ComparatorBlockActor(context));
                original.Generators.Register<VoidGenerator>("void");
                original.Generators.SetDefault("void");
                World world = original.CreateWorld("test", configure => configure.CreateDimension("minecraft:overworld"), new LevelDbProvider(path));
                Dimension dimension = world.GetDimension("minecraft:overworld")!;
                Chunk chunk = dimension.GetOrCreateChunk(0, 0);
                dimension.SetPermutation(4, 99, 4, stone);
                dimension.SetPermutation(4, 100, 4, repeater);
                dimension.SetPermutation(4, 100, 3, poweredLever);
                dimension.SetPermutation(8, 99, 4, stone);
                dimension.SetPermutation(8, 100, 4, torch);
                dimension.SetPermutation(9, 99, 4, poweredLever);
                dimension.SetPermutation(12, 100, 2, comparator);
                dimension.SetPermutation(14, 99, 2, stone);
                dimension.SetPermutation(14, 100, 2, daylightDetector);
                CompoundTag comparatorNbt = new();
                comparatorNbt.Add("id", new StringTag { Value = "Comparator" });
                comparatorNbt.Add("OutputSignal", new IntTag { Value = 11 });
                chunk.SetBlockNbt(12, 100, 2, comparatorNbt);
                dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
                dimension.ScheduleBlockUpdate(4, 100, 4, 2);
                dimension.ScheduleBlockUpdate(8, 100, 4, 2);
                dimension.ScheduleBlockUpdate(14, 100, 2, 20);
            }

            using Server resumed = new(new ServerProperties { DefaultWorldName = "test" });
            resumed.BlockActors.Register("minecraft:powered_comparator", static context => new ComparatorBlockActor(context));
            resumed.BlockActors.Register("minecraft:unpowered_comparator", static context => new ComparatorBlockActor(context));
            resumed.Generators.Register<VoidGenerator>("void");
            resumed.Generators.SetDefault("void");
            World resumedWorld = resumed.CreateWorld("test", configure => configure.CreateDimension("minecraft:overworld"), new LevelDbProvider(path));
            Dimension resumedDimension = resumedWorld.GetDimension("minecraft:overworld")!;
            resumedDimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
            Assert.Equal(ChunkRequestStatus.Queued, resumedDimension.Request(0, 0));

            Stopwatch wait = Stopwatch.StartNew();
            while (resumedDimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                resumedDimension.Tick();
                Thread.Sleep(1);
            }

            Chunk loaded = resumedDimension.GetChunk(0, 0)
                ?? throw new Xunit.Sdk.XunitException("LevelDB device chunk load did not complete.");
            Assert.Equal(3, loaded.PendingTicks!.Get<ListTag>("tickList")!.Value.Count);
            Assert.Equal("minecraft:unpowered_repeater", resumedDimension.GetPermutation(4, 100, 4).Identifier);
            Assert.Equal("minecraft:redstone_torch", resumedDimension.GetPermutation(8, 100, 4).Identifier);
            Assert.Equal("minecraft:daylight_detector", resumedDimension.GetPermutation(14, 100, 2).Identifier);
            Assert.Equal(11, resumedDimension.Redstone.InputPower(resumedDimension, 12, 100, 2, 12, 100, 3));

            for (int tick = 0; tick < 4; tick++)
                resumedDimension.Tick();

            Assert.Equal("minecraft:powered_repeater", resumedDimension.GetPermutation(4, 100, 4).Identifier);
            Assert.Equal("minecraft:unlit_redstone_torch", resumedDimension.GetPermutation(8, 100, 4).Identifier);
            for (int tick = 4; tick < 20; tick++)
                resumedDimension.Tick();

            CompoundTag daylightTick = Assert.IsType<CompoundTag>(loaded.PendingTicks!.Get<ListTag>("tickList")!.Value.Single(record =>
                record is CompoundTag value && value.Get<IntTag>("x")?.Value == 14));
            Assert.Equal(40, daylightTick.Get<LongTag>("time")!.Value);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CleanWorldShutdownPersistsTheLatestActiveChunkClock() {

        string root = Path.Combine(Path.GetTempPath(), "basalt-scheduled-clock-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "db");
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");

        try {
            using (World original = new("test", generators, provider: new LevelDbProvider(path))) {
                Dimension dimension = original.CreateDimension("minecraft:overworld");
                Chunk chunk = dimension.GetOrCreateChunk(0, 0);
                dimension.SetPermutation(1, 100, 1, BlockPalette.FindDefaultPermutation("minecraft:observer")!);
                dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
                dimension.ScheduleBlockUpdate(1, 100, 1, 100);
                for (int tick = 0; tick < 7; tick++)
                    dimension.Tick();

                Assert.Equal(0, chunk.PendingTicks!.Get<IntTag>("currentTick")!.Value);
            }

            using World resumed = new("test", generators, provider: new LevelDbProvider(path));
            Dimension resumedDimension = resumed.CreateDimension("minecraft:overworld");
            Assert.Equal(ChunkRequestStatus.Queued, resumedDimension.Request(0, 0));
            Stopwatch wait = Stopwatch.StartNew();
            while (resumedDimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                resumedDimension.Tick();
                Thread.Sleep(1);
            }

            Chunk restored = resumedDimension.GetChunk(0, 0)
                ?? throw new Xunit.Sdk.XunitException("LevelDB clock chunk load did not complete.");
            Assert.Equal(7, restored.PendingTicks!.Get<IntTag>("currentTick")!.Value);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void DaylightDetectorResumesTheNativeWorldClockAfterRestart() {
        if (Interlocked.Exchange(ref daylightDetectorRegistered, 1) == 0)
            BlockPalette.RegisterTrait<DaylightDetectorTrait>();

        string root = Path.Combine(Path.GetTempPath(), "basalt-world-time-restart-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "db");
        ChunkGeneratorRegistry generators = new();
        generators.Register<VoidGenerator>("void");
        generators.SetDefault("void");
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation detector = BlockPalette.FindDefaultPermutation("minecraft:daylight_detector")!;

        try {
            using (LevelDbProvider provider = new(path)) {
                CompoundTag initialWorldData = new();
                initialWorldData.Add("Time", new LongTag { Value = 5999 });
                initialWorldData.Add("currentTick", new LongTag { Value = 5999 });
                initialWorldData.Add("unknownVanillaField", new StringTag { Value = "preserved" });
                using BinaryStream stream = BinaryStream.Rent(initialWorldData.GetSize());
                BinaryWriter writer = stream.GetWriter();
                BasaltBE.Nbt.Nbt.Write(initialWorldData, ref writer);
                provider.WriteWorldData(stream.Buffer.Span[..stream.Offset]);
            }

            using (World original = new("test", generators, provider: new LevelDbProvider(path))) {
                Dimension dimension = original.CreateDimension("minecraft:overworld");
                Chunk chunk = dimension.GetOrCreateChunk(0, 0);
                dimension.SetPermutation(14, 99, 2, stone);
                dimension.SetPermutation(14, 100, 2, detector);
                dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
                Assert.Equal(5999, original.GameTime);
                dimension.ScheduleBlockUpdate(14, 100, 2, 20);
                original.Tick();
                Assert.Equal(6000, original.GameTime);
                Assert.Equal(0, chunk.PendingTicks!.Get<IntTag>("currentTick")!.Value);
            }

            using (LevelDbProvider provider = new(path)) {
                byte[] data = provider.ReadWorldData()!;
                int offset = 0;
                CompoundTag savedWorldData = Assert.IsType<CompoundTag>(BasaltBE.Nbt.Nbt.Read(data, ref offset));
                Assert.Equal(data.Length, offset);
                Assert.Equal(6000, savedWorldData.Get<LongTag>("Time")!.Value);
                Assert.Equal(6000, savedWorldData.Get<LongTag>("currentTick")!.Value);
                Assert.Equal("preserved", savedWorldData.Get<StringTag>("unknownVanillaField")!.Value);
            }

            using World resumed = new("test", generators, provider: new LevelDbProvider(path));
            Assert.Equal(6000, resumed.GameTime);
            Dimension resumedDimension = resumed.CreateDimension("minecraft:overworld");
            resumedDimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
            Assert.Equal(ChunkRequestStatus.Queued, resumedDimension.Request(0, 0));
            Stopwatch wait = Stopwatch.StartNew();
            while (resumedDimension.GetChunk(0, 0) is null && wait.Elapsed < TimeSpan.FromSeconds(5)) {
                resumedDimension.Tick();
                Thread.Sleep(1);
            }

            Assert.NotNull(resumedDimension.GetChunk(0, 0));
            for (int tick = 0; tick < 25; tick++)
                resumed.Tick();

            Assert.Equal(6025, resumed.GameTime);
            Assert.Equal(15, resumedDimension.GetPermutation(14, 100, 2).State.Values.GetValueOrDefault("redstone_signal"));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ScheduledTicksResumeAndDispatchInDeadlineOrder() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        ScheduledTickProbeTrait.Executions = 0;
        ScheduledTickProbeTrait.Positions = [];
        using Dimension original = new("test:original", new VoidGenerator());
        Chunk originalChunk = original.GetOrCreateChunk(0, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation dirt = BlockPalette.FindDefaultPermutation("minecraft:dirt")!;
        original.SetPermutation(1, 0, 1, stone);
        original.SetPermutation(2, 0, 1, stone);
        original.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        original.ScheduleBlockUpdate(1, 0, 1, 2);
        original.ScheduleBlockUpdate(2, 0, 1, 2);
        original.Tick();
        Assert.Equal(0, ScheduledTickProbeTrait.Executions);

        original.PrepareScheduledTicks(originalChunk);
        Chunk savedChunk = Chunk.Deserialize(0, 0, originalChunk.Serialize(), original);
        Assert.NotNull(savedChunk.PendingTicks);

        using Dimension restored = new("test:restored", new VoidGenerator());
        Chunk restoredChunk = restored.GetOrCreateChunk(0, 0);
        restored.SetPermutation(1, 0, 1, stone);
        restored.SetPermutation(2, 0, 1, stone);
        restoredChunk.PendingTicks = savedChunk.PendingTicks;
        restored.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        restored.Tick();
        Assert.Equal(2, ScheduledTickProbeTrait.Executions);
        Assert.Equal([1, 2], ScheduledTickProbeTrait.Positions);
        ScheduledTickProbeTrait.Positions = null;

        restored.SetPermutation(3, 0, 1, stone);
        restored.ScheduleBlockUpdate(3, 0, 1, 3);
        long revision = restoredChunk.Revision;
        restored.Tick();
        Assert.Equal(2, ScheduledTickProbeTrait.Executions);
        restored.RemoveTickingArea("test");
        Assert.True(restoredChunk.Revision > revision);
        for (int tick = 0; tick < 5; tick++)
            restored.Tick();
        Assert.Equal(2, ScheduledTickProbeTrait.Executions);
        restored.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        restored.Tick();
        Assert.Equal(2, ScheduledTickProbeTrait.Executions);
        restored.Tick();
        Assert.Equal(3, ScheduledTickProbeTrait.Executions);

        const int warmupCount = 5;
        const int sampleCount = 100;
        int expectedExecutions = 3;
        foreach (int updateCount in new[] { 1, 200, 1000 }) {
            double[] scheduleSamples = new double[sampleCount];
            double[] dispatchSamples = new double[sampleCount];
            long allocated = 0;
            for (int sample = -warmupCount; sample < sampleCount; sample++) {
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long scheduleStart = Stopwatch.GetTimestamp();
                for (int index = 0; index < updateCount; index++) {
                    int x = index & 15;
                    int y = index >> 8;
                    int z = index >> 4 & 15;
                    restored.SetPermutation(x, y, z, stone);
                    restored.ScheduleBlockUpdate(x, y, z, 1);
                }
                long scheduleEnd = Stopwatch.GetTimestamp();
                int targetExecutions = ScheduledTickProbeTrait.Executions + updateCount;
                while (ScheduledTickProbeTrait.Executions < targetExecutions)
                    restored.Tick();
                long dispatchEnd = Stopwatch.GetTimestamp();
                if (sample >= 0) {
                    scheduleSamples[sample] = Stopwatch.GetElapsedTime(scheduleStart, scheduleEnd).TotalMilliseconds;
                    dispatchSamples[sample] = Stopwatch.GetElapsedTime(scheduleEnd, dispatchEnd).TotalMilliseconds;
                    allocated += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                }
                expectedExecutions += updateCount;
                Assert.Equal(expectedExecutions, ScheduledTickProbeTrait.Executions);
            }
            Array.Sort(scheduleSamples);
            Array.Sort(dispatchSamples);
            double scheduleMedian = (scheduleSamples[49] + scheduleSamples[50]) / 2;
            double dispatchMedian = (dispatchSamples[49] + dispatchSamples[50]) / 2;
            output.WriteLine(
                $"{updateCount} scheduled block ticks, 5 warmups, 100 samples: schedule median {scheduleMedian:F3} ms, " +
                $"p95 {scheduleSamples[94]:F3} ms, p99 {scheduleSamples[98]:F3} ms, max {scheduleSamples[99]:F3} ms; " +
                $"dispatch-to-drain median {dispatchMedian:F3} ms, p95 {dispatchSamples[94]:F3} ms, " +
                $"p99 {dispatchSamples[98]:F3} ms, max {dispatchSamples[99]:F3} ms; " +
                $"allocation per sample {(allocated / (double)sampleCount / 1024):F1} KiB."
            );

            double[] cancelSamples = new double[sampleCount];
            long cancelAllocated = 0;
            for (int sample = -warmupCount; sample < sampleCount; sample++) {
                for (int index = 0; index < updateCount; index++) {
                    int x = index & 15;
                    int y = index >> 8;
                    int z = index >> 4 & 15;
                    restored.SetPermutation(x, y, z, stone);
                    restored.ScheduleBlockUpdate(x, y, z, 100);
                }

                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long cancelStart = Stopwatch.GetTimestamp();
                for (int index = 0; index < updateCount; index++) {
                    int x = index & 15;
                    int y = index >> 8;
                    int z = index >> 4 & 15;
                    restored.SetPermutation(x, y, z, dirt);
                }
                restored.PrepareScheduledTicks(restoredChunk);
                Assert.Null(restoredChunk.PendingTicks);
                long cancelEnd = Stopwatch.GetTimestamp();
                if (sample >= 0) {
                    cancelSamples[sample] = Stopwatch.GetElapsedTime(cancelStart, cancelEnd).TotalMilliseconds;
                    cancelAllocated += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                }
            }

            Array.Sort(cancelSamples);
            double cancelMedian = (cancelSamples[49] + cancelSamples[50]) / 2;
            output.WriteLine(
                $"Cancel {updateCount} scheduled block ticks by replacing their blocks, 5 warmups, 100 samples: " +
                $"median {cancelMedian:F3} ms, p95 {cancelSamples[94]:F3} ms, " +
                $"p99 {cancelSamples[98]:F3} ms, max {cancelSamples[99]:F3} ms; " +
                $"allocation per sample {(cancelAllocated / (double)sampleCount / 1024):F1} KiB."
            );
        }
    }

    [Fact]
    public void EarlierRescheduleKeepsUnknownFieldsAndUpdatesBlockIdentity() {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            BlockPalette.RegisterTrait<ScheduledTickProbeTrait>();

        using Dimension dimension = new("test:reschedule", new VoidGenerator());
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.SetPermutation(1, 0, 1, stone);

        CompoundTag record = new();
        record.Add("blockState", BlockStorage.CreatePermutationTag(stone));
        record.Add("time", new LongTag { Value = 20 });
        record.Add("x", new IntTag { Value = 1 });
        record.Add("y", new IntTag { Value = 0 });
        record.Add("z", new IntTag { Value = 1 });
        record.Add("extension", new StringTag { Value = "preserve" });
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 10 });
        pending.Add("tickList", new ListTag { Value = [record] });
        chunk.PendingTicks = pending;
        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);

        BlockPermutation dirt = BlockPalette.FindDefaultPermutation("minecraft:dirt")!;
        chunk.SetPermutation(1, 0, 1, dirt);
        dimension.ScheduleBlockUpdate(1, 0, 1, 2);
        dimension.Tick();
        dimension.PrepareScheduledTicks(chunk);

        CompoundTag savedRecord = Assert.IsType<CompoundTag>(Assert.Single(chunk.PendingTicks!.Get<ListTag>("tickList")!.Value));
        Assert.Equal("preserve", savedRecord.Get<StringTag>("extension")!.Value);
        Assert.Equal(12, savedRecord.Get<LongTag>("time")!.Value);
        Assert.Equal("minecraft:dirt", savedRecord.Get<CompoundTag>("blockState")!.Get<StringTag>("name")!.Value);
        Assert.Equal(11, chunk.PendingTicks.Get<IntTag>("currentTick")!.Value);
    }

    [Fact]
    public void ReplacingABlockCancelsOnlyItsIndexedScheduledTicks() {

        using Dimension dimension = new("test:cancel-position", new VoidGenerator());
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        dimension.SetPermutation(1, 0, 1, observer);
        dimension.SetPermutation(2, 0, 1, observer);
        dimension.ScheduleBlockUpdate(1, 0, 1, 20);
        dimension.ScheduleBlockUpdate(2, 0, 1, 20);

        dimension.SetPermutation(1, 0, 1, stone);
        dimension.PrepareScheduledTicks(chunk);

        CompoundTag remaining = Assert.IsType<CompoundTag>(
            Assert.Single(chunk.PendingTicks!.Get<ListTag>("tickList")!.Value)
        );
        Assert.Equal(2, remaining.Get<IntTag>("x")!.Value);
    }

    [Fact]
    public void ChangingBlockStateDoesNotCancelItsScheduledTick() {

        using Dimension dimension = new("test:state-change", new VoidGenerator());
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        dimension.SetPermutation(1, 0, 1, observer);
        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        dimension.ScheduleBlockUpdate(1, 0, 1, 1);
        BlockPermutation rotated = BlockPalette.Find(
            observer.Identifier,
            observer.State,
            "minecraft:facing_direction",
            "north"
        )!;

        dimension.SetPermutation(1, 0, 1, rotated);
        dimension.Tick();

        BlockPermutation updated = dimension.GetPermutation(1, 0, 1);
        Assert.Equal("minecraft:observer", updated.Identifier);
        Assert.Equal("north", updated.State.Values.GetValueOrDefault("minecraft:facing_direction"));
        Assert.True(updated.State.Values.GetValueOrDefault("powered_bit") is true);
    }

    [Fact]
    public void RedstoneWirePowerCrossesChunkBoundary() {
        using Dimension dimension = new("test:redstone-boundary", new VoidGenerator());
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        const int firstWireX = 12;
        const int wireCount = 200;
        const int y = 0;
        const int z = 8;

        dimension.SetPermutation(firstWireX - 1, y, z, poweredLever);
        for (int index = 0; index < wireCount; index++) {
            int x = firstWireX + index;
            dimension.SetPermutation(x, y - 1, z, stone);
            dimension.SetPermutation(x, y, z, wire);
        }
        dimension.Redstone.Update(dimension);

        for (int index = 0; index < wireCount; index++) {
            int expectedPower = Math.Max(0, 15 - index);
            BlockPermutation current = dimension.GetPermutation(firstWireX + index, y, z);
            Assert.Equal(expectedPower, current.State.Values.GetValueOrDefault("redstone_signal"));
        }

        BlockPermutation unpoweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", false)!;
        const int warmupCount = 5;
        const int sampleCount = 20;
        double[] samples = new double[sampleCount];
        long allocated = 0;
        BlockPermutation currentLever = poweredLever;
        for (int sample = -warmupCount; sample < sampleCount; sample++) {
            currentLever = currentLever == poweredLever ? unpoweredLever : poweredLever;
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            dimension.SetPermutation(firstWireX - 1, y, z, currentLever);
            dimension.Redstone.Update(dimension);
            long end = Stopwatch.GetTimestamp();
            if (sample >= 0) {
                samples[sample] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
                allocated += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            }

            int expectedPower = currentLever == poweredLever ? 15 : 0;
            Assert.Equal(expectedPower, dimension.GetPermutation(firstWireX, y, z).State.Values.GetValueOrDefault("redstone_signal"));
            Assert.Equal(expectedPower == 0 ? 0 : 1, dimension.GetPermutation(firstWireX + 14, y, z).State.Values.GetValueOrDefault("redstone_signal"));
            Assert.Equal(0, dimension.GetPermutation(firstWireX + 15, y, z).State.Values.GetValueOrDefault("redstone_signal"));
        }

        Array.Sort(samples);
        double median = (samples[9] + samples[10]) / 2;
        output.WriteLine(
            $"Toggle one lever across {wireCount} wires and 14 chunk columns, 5 warmups, 20 samples: " +
            $"redstone update median {median:F3} ms, p95 {samples[18]:F3} ms; " +
            $"allocation per toggle {(allocated / (double)sampleCount / 1024):F1} KiB. No viewers attached."
        );
    }

    [Fact]
    public void RedstoneWireRecalculatesWhenNeighborChunkLoads() {

        using Dimension dimension = new("test:redstone-late-neighbor", new VoidGenerator());
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        BlockPermutation observer = BlockPalette.FindDefaultPermutation("minecraft:observer")!;
        BlockPermutation eastObserver = BlockPalette.Find(observer.Identifier, observer.State, "minecraft:facing_direction", "east")!;
        const int wireY = 20;
        dimension.SetPermutation(15, wireY, 8, wire);
        dimension.Redstone.Update(dimension);
        Assert.Equal(0, dimension.GetPermutation(15, wireY, 8).State.Values.GetValueOrDefault("redstone_signal"));

        dimension.GetChunk(0, 0)!.SetPermutation(14, wireY, 8, eastObserver);
        Chunk sourceChunk = dimension.GetOrCreateChunk(1, 0);
        sourceChunk.SetPermutation(0, wireY, 8, poweredLever);
        dimension.Redstone.ChunkLoaded(dimension, sourceChunk);
        dimension.Redstone.Update(dimension);

        Assert.Equal(15, dimension.GetPermutation(15, wireY, 8).State.Values.GetValueOrDefault("redstone_signal"));
        Assert.Null(dimension.GetChunk(0, 0)!.PendingTicks);

        const int warmupCount = 5;
        const int sampleCount = 100;
        double[] samples = new double[sampleCount];
        long allocated = 0;
        for (int sample = -warmupCount; sample < sampleCount; sample++) {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            dimension.Redstone.ChunkLoaded(dimension, sourceChunk);
            dimension.Redstone.Update(dimension);
            long end = Stopwatch.GetTimestamp();
            if (sample >= 0) {
                samples[sample] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
                allocated += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            }
        }

        Array.Sort(samples);
        output.WriteLine(
            $"Reconcile one loaded source chunk next to one wire, {warmupCount} warmups, {sampleCount} samples: " +
            $"median {((samples[49] + samples[50]) / 2):F3} ms, p95 {samples[94]:F3} ms, " +
            $"p99 {samples[98]:F3} ms, max {samples[^1]:F3} ms, " +
            $"allocation {allocated / (double)sampleCount / 1024:F1} KiB. No viewers attached."
        );
    }

    [Fact]
    public void UnknownChunkBoundaryPreservesSavedWirePowerUntilNeighborLoads() {
        using Dimension dimension = new("test:redstone-unknown-boundary", new VoidGenerator());
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation eastSideWire = BlockPalette.Find(wire.Identifier, wire.State, "redstone_east", "side")!;
        BlockPermutation poweredWire = BlockPalette.Find(eastSideWire.Identifier, eastSideWire.State, "redstone_signal", 5)!;
        Chunk wireChunk = dimension.GetOrCreateChunk(0, 0);
        wireChunk.SetPermutation(15, 20, 8, poweredWire);
        dimension.Redstone.MarkDirty(15, 20, 8);
        dimension.Redstone.Update(dimension);
        Assert.Equal(5, dimension.GetPermutation(15, 20, 8).State.Values.GetValueOrDefault("redstone_signal"));
        Assert.Equal("side", dimension.GetPermutation(15, 20, 8).State.Values.GetValueOrDefault("redstone_east"));

        Chunk emptyNeighbor = dimension.GetOrCreateChunk(1, 0);
        dimension.Redstone.ChunkLoaded(dimension, emptyNeighbor);
        dimension.Redstone.Update(dimension);
        Assert.Equal(0, dimension.GetPermutation(15, 20, 8).State.Values.GetValueOrDefault("redstone_signal"));
        Assert.Equal("none", dimension.GetPermutation(15, 20, 8).State.Values.GetValueOrDefault("redstone_east"));
    }

    [Fact]
    public void RedstoneWireUpdatesKnownNetworkWhileNeighborChunkIsUnknown() {
        using Dimension dimension = new("test:redstone-known-interior", new VoidGenerator());
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        const int wireY = 20;

        dimension.SetPermutation(9, wireY, 8, poweredLever);
        for (int x = 10; x <= 15; x++)
            dimension.SetPermutation(x, wireY, 8, wire);

        dimension.Redstone.Update(dimension);
        Assert.Equal(15, dimension.GetPermutation(10, wireY, 8).State.Values.GetValueOrDefault("redstone_signal"));
        Assert.Equal(10, dimension.GetPermutation(15, wireY, 8).State.Values.GetValueOrDefault("redstone_signal"));

        BlockPermutation unpoweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", false)!;
        dimension.SetPermutation(9, wireY, 8, unpoweredLever);
        dimension.Redstone.Update(dimension);

        for (int x = 10; x <= 15; x++)
            Assert.Equal(0, dimension.GetPermutation(x, wireY, 8).State.Values.GetValueOrDefault("redstone_signal"));
    }

    [Fact]
    public void DeviceInputRemainsUnknownUntilNeighborChunkLoads() {
        using Dimension dimension = new("test:redstone-device-unknown", new VoidGenerator());
        dimension.GetOrCreateChunk(0, 0);

        Assert.Null(dimension.Redstone.InputPower(dimension, 15, 20, 8, 15, 20, 9));
        Assert.Null(dimension.GetLoadedPermutation(16, 20, 8));

        dimension.GetOrCreateChunk(1, 0);

        Assert.Equal(0, dimension.Redstone.InputPower(dimension, 15, 20, 8, 15, 20, 9));
        Assert.NotNull(dimension.GetLoadedPermutation(16, 20, 8));
    }

    [Fact]
    public void RepeaterAndComparatorKeepStateWhileInputsAreUnknown() {
        using Dimension dimension = new("test:redstone-device-preserves-state", new VoidGenerator());
        RedstoneDeviceTrait trait = new();
        BlockPermutation repeater = BlockPalette.FindDefaultPermutation("minecraft:unpowered_repeater")!;
        repeater = BlockPalette.Find(repeater.Identifier, repeater.State, "minecraft:cardinal_direction", "east")!;
        BlockPermutation comparator = BlockPalette.FindDefaultPermutation("minecraft:powered_comparator")!;
        comparator = BlockPalette.Find(comparator.Identifier, comparator.State, "minecraft:cardinal_direction", "east")!;
        dimension.SetPermutation(15, 20, 8, repeater);
        dimension.SetPermutation(15, 20, 10, comparator);

        trait.OnScheduledUpdate(dimension, 15, 20, 8, repeater);
        trait.OnScheduledUpdate(dimension, 15, 20, 10, comparator);

        Assert.Same(repeater, dimension.GetPermutation(15, 20, 8));
        Assert.Same(comparator, dimension.GetPermutation(15, 20, 10));
    }

    [Fact]
    public void RepeaterReadsPowerFromItsCardinalDirectionAndOutputsOpposite() {
        using Dimension dimension = new("test:repeater-direction", new VoidGenerator());
        BlockPermutation repeater = BlockPalette.FindDefaultPermutation("minecraft:unpowered_repeater")!;
        repeater = BlockPalette.Find(repeater.Identifier, repeater.State, "minecraft:cardinal_direction", "north")!;
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation poweredWire = BlockPalette.Find(wire.Identifier, wire.State, "redstone_signal", 15)!;
        dimension.SetPermutation(8, 20, 8, repeater);
        dimension.SetPermutation(8, 20, 7, poweredWire);

        RedstoneDeviceTrait trait = new();
        trait.OnScheduledUpdate(dimension, 8, 20, 8, repeater);
        BlockPermutation poweredRepeater = dimension.GetPermutation(8, 20, 8);
        Assert.Equal("minecraft:powered_repeater", poweredRepeater.Identifier);
        Assert.True(dimension.Redstone.BlockPowered(dimension, 8, 20, 9));
        Assert.False(dimension.Redstone.BlockPowered(dimension, 8, 20, 7));

        dimension.SetPermutation(8, 20, 7, wire);
        dimension.SetPermutation(8, 20, 9, poweredWire);
        trait.OnScheduledUpdate(dimension, 8, 20, 8, poweredRepeater);
        Assert.Equal("minecraft:unpowered_repeater", dimension.GetPermutation(8, 20, 8).Identifier);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(3, 8)]
    public void RepeaterRespondsAfterConfiguredDelay(int repeaterDelay, int expectedTicks) {
        if (Interlocked.Exchange(ref redstoneDevicesRegistered, 1) == 0)
            BlockPalette.RegisterTrait<RedstoneDeviceTrait>();
        if (Interlocked.Exchange(ref redstoneWireRegistered, 1) == 0)
            BlockPalette.RegisterTrait<RedstoneWireTrait>();

        using Dimension dimension = new("test:repeater-signal-delay", new VoidGenerator());
        dimension.GetOrCreateChunk(0, 0);
        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);

        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        BlockPermutation wire = BlockPalette.FindDefaultPermutation("minecraft:redstone_wire")!;
        BlockPermutation repeater = BlockPalette.FindDefaultPermutation("minecraft:unpowered_repeater")!;
        repeater = BlockPalette.Find(repeater.Identifier, repeater.State, "repeater_delay", repeaterDelay)!;
        repeater = BlockPalette.Find(repeater.Identifier, repeater.State, "minecraft:cardinal_direction", "west")!;
        dimension.SetPermutation(8, 20, 8, lever);
        dimension.SetPermutation(9, 20, 8, wire);
        dimension.SetPermutation(10, 20, 8, repeater);
        dimension.Redstone.Update(dimension);

        dimension.SetPermutation(8, 20, 8, poweredLever);
        dimension.Redstone.Update(dimension);
        Assert.Equal(15, dimension.GetPermutation(9, 20, 8).State.Values.GetValueOrDefault("redstone_signal"));
        Assert.Equal("minecraft:unpowered_repeater", dimension.GetPermutation(10, 20, 8).Identifier);

        for (int tick = 1; tick < expectedTicks; tick++) {
            dimension.Tick();
            Assert.Equal("minecraft:unpowered_repeater", dimension.GetPermutation(10, 20, 8).Identifier);
        }

        dimension.Tick();
        Assert.Equal("minecraft:powered_repeater", dimension.GetPermutation(10, 20, 8).Identifier);
    }

    [Fact]
    public void RepeaterPlacementUsesCardinalYawMapping() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        BlockPermutation support = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation repeater = BlockPalette.FindDefaultPermutation("minecraft:unpowered_repeater")!;
        fixture.Dimension.SetPermutation(8, 19, 8, support);

        bool placed = new RedstoneDeviceTrait().OnPlace(player, 8, 20, 8, 1, new Vec3(), ref repeater);

        Assert.True(placed);
        Assert.Equal("north", repeater.State.Values.GetValueOrDefault("minecraft:cardinal_direction"));
    }

    [Fact]
    public void TorchDoesNotDropOrChangeStateAtUnknownChunkBoundary() {
        if (Interlocked.Exchange(ref redstoneDevicesRegistered, 1) == 0) {
            BlockPalette.RegisterTrait<RedstoneDeviceTrait>();
            BlockPalette.RegisterTrait<RedstoneTorchTrait>();
        }

        using Dimension dimension = new("test:redstone-torch-unknown-support", new VoidGenerator());
        BlockPermutation torch = BlockPalette.FindDefaultPermutation("minecraft:redstone_torch")!;
        torch = BlockPalette.Find(torch.Identifier, torch.State, "torch_facing_direction", "east")!;
        dimension.SetPermutation(15, 20, 8, torch);

        Assert.False(new RedstoneTorchTrait().OnNeighborUpdate(dimension, 15, 20, 8));
        Assert.Same(torch, dimension.GetPermutation(15, 20, 8));

        Chunk supportChunk = dimension.GetOrCreateChunk(1, 0);
        supportChunk.SetPermutation(0, 20, 8, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        dimension.Redstone.ChunkLoaded(dimension, supportChunk);
        Assert.NotNull(dimension.GetChunk(0, 0)!.PendingTicks);
    }

    [Fact]
    public void ButtonAndLeverSupportIsRecheckedWhenNeighborChunkLoads() {
        if (Interlocked.Exchange(ref leverRegistered, 1) == 0)
            BlockPalette.RegisterTrait<LeverTrait>();

        using Dimension dimension = new("test:redstone-support-load", new VoidGenerator());
        BlockPermutation button = BlockPalette.FindDefaultPermutation("minecraft:stone_button")!;
        button = BlockPalette.Find(button.Identifier, button.State, "facing_direction", 5)!;
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        lever = BlockPalette.Find(lever.Identifier, lever.State, "lever_direction", "east")!;
        dimension.SetPermutation(0, 20, 8, button);
        dimension.SetPermutation(0, 20, 10, lever);

        Assert.False(new ButtonTrait().OnNeighborUpdate(dimension, 0, 20, 8));
        Assert.False(new LeverTrait().OnNeighborUpdate(dimension, 0, 20, 10));
        Assert.Same(button, dimension.GetPermutation(0, 20, 8));
        Assert.Same(lever, dimension.GetPermutation(0, 20, 10));

        Chunk neighborChunk = dimension.GetOrCreateChunk(-1, 0);
        dimension.Redstone.ChunkLoaded(dimension, neighborChunk);

        Assert.Same(BlockStorage.Air, dimension.GetPermutation(0, 20, 8));
        Assert.Same(BlockStorage.Air, dimension.GetPermutation(0, 20, 10));
    }

    [Fact]
    public void ComparatorOutputSignalSurvivesPermutationAndChunkRoundTrip() {
        using Dimension dimension = new("test:comparator-storage", new VoidGenerator());
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        BlockPermutation unpowered = BlockPalette.FindDefaultPermutation("minecraft:unpowered_comparator")!;
        BlockPermutation powered = BlockPalette.FindDefaultPermutation("minecraft:powered_comparator")!;

        for (int signal = 1; signal <= 14; signal++) {
            int x = signal;
            dimension.SetPermutation(x, 0, 0, unpowered);
            CompoundTag nbt = new();
            nbt.Add("id", new StringTag { Value = "Comparator" });
            nbt.Add("OutputSignal", new IntTag { Value = signal });
            chunk.SetBlockNbt(x, 0, 0, nbt);
            dimension.SetPermutation(x, 0, 0, powered);
            dimension.SetPermutation(x, 0, 0, unpowered);
        }

        Chunk restored = Chunk.Deserialize(0, 0, chunk.Serialize(), dimension);
        for (int signal = 1; signal <= 14; signal++) {
            CompoundTag nbt = restored.GetBlockNbt(signal, 0, 0)!;
            ComparatorBlockActor actor = new(new BlockActorContext(
                dimension,
                new BlockPos { X = signal, Y = 0, Z = 0 },
                "minecraft:unpowered_comparator",
                unpowered,
                nbt
            ));
            actor.OnRead(nbt);
            Assert.Equal(signal, actor.OutputSignal);
            CompoundTag saved = new();
            actor.OnWrite(saved);
            Assert.Equal(signal, saved.Get<IntTag>("OutputSignal")!.Value);
        }
    }

    [BlockTraitAppliesTo("minecraft:stone", "minecraft:dirt")]
    public sealed class ScheduledTickProbeTrait : BlockTrait {
        internal static int Executions;
        internal static int CallbackRuns;
        internal static List<int>? Positions;
        internal static Action<Dimension, int, int, int>? Callback;
        internal override bool HasScheduledUpdates => true;

        public override void OnScheduledUpdate(Dimension dimension, int x, int y, int z) {
            Positions?.Add(x);
            Interlocked.Increment(ref Executions);
            Callback?.Invoke(dimension, x, y, z);
        }
    }

    [BlockTraitAppliesTo("minecraft:stone")]
    public sealed class ManyChunkScheduledTickTrait : BlockTrait {
        internal static int Executions;
        internal static int CurrentTick;
        internal static int[,] ChunkExecutions = new int[1, 1];
        internal override bool HasScheduledUpdates => true;

        public override void OnScheduledUpdate(Dimension dimension, int x, int y, int z) {
            Executions++;
            ChunkExecutions[CurrentTick, x >> 4]++;
        }
    }

    sealed class DelayedChunkProvider(ChunkStorageEntry[] entries) : IWorldProvider {
        readonly ManualResetEventSlim readStarted = new();
        readonly ManualResetEventSlim releaseRead = new();

        public string Identifier => "delayed-test";
        public ManualResetEventSlim ReadStarted => readStarted;
        public ManualResetEventSlim ReleaseRead => releaseRead;

        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
            readStarted.Set();
            if (!releaseRead.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("The test did not release its delayed chunk read.");
            return entries;
        }

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) { }
        public bool DeleteChunk(ChunkStorageKey key) => false;
        public void Flush() { }

        public void Dispose() {
            readStarted.Dispose();
            releaseRead.Dispose();
        }
    }

    sealed class GatedWriteProvider : IWorldProvider {
        readonly ManualResetEventSlim writeStarted = new();
        readonly ManualResetEventSlim releaseWrite = new();
        readonly ConcurrentDictionary<ChunkStorageKey, ChunkStorageEntry[]> chunks = new();
        ChunkStorageEntry[]? lastEntries;
        int writeCalls;
        int writeCount;

        public string Identifier => "gated-write-test";
        public ManualResetEventSlim WriteStarted => writeStarted;
        public ManualResetEventSlim ReleaseWrite => releaseWrite;
        public int WriteCount => Volatile.Read(ref writeCount);
        public ConcurrentDictionary<ChunkStorageKey, ChunkStorageEntry[]> Chunks => chunks;
        public ChunkStorageEntry[]? LastEntries => Volatile.Read(ref lastEntries);
        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) => chunks.GetValueOrDefault(key);

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) {
            if (Interlocked.Increment(ref writeCalls) == 1) {
                writeStarted.Set();
                if (!releaseWrite.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The test did not release its delayed chunk write.");
            }

            foreach (ChunkStorageRecord chunk in chunks) {
                this.chunks[chunk.Key] = chunk.Entries;
                Volatile.Write(ref lastEntries, chunk.Entries);
            }
            Interlocked.Increment(ref writeCount);
        }

        public bool DeleteChunk(ChunkStorageKey key) => false;
        public void Flush() { }

        public void Dispose() {
            writeStarted.Dispose();
            releaseWrite.Dispose();
        }
    }

    sealed class FailingWriteProvider : IWorldProvider {
        readonly ManualResetEventSlim firstWriteStarted = new();
        ChunkStorageEntry[]? lastEntries;
        int writeCount;

        public string Identifier => "failing-write-test";
        public ManualResetEventSlim FirstWriteStarted => firstWriteStarted;
        public int WriteCount => Volatile.Read(ref writeCount);
        public ChunkStorageEntry[]? LastEntries => Volatile.Read(ref lastEntries);
        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) => null;

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) {
            if (Interlocked.Increment(ref writeCount) == 1) {
                firstWriteStarted.Set();
                throw new IOException("Expected test failure.");
            }

            if (chunks.Length > 0)
                Volatile.Write(ref lastEntries, chunks[^1].Entries);
        }

        public bool DeleteChunk(ChunkStorageKey key) => false;
        public void Flush() { }
        public void Dispose() => firstWriteStarted.Dispose();
    }

    sealed class FailingReadProvider : IWorldProvider {
        readonly ManualResetEventSlim readStarted = new();

        public string Identifier => "failing-read-test";
        public ManualResetEventSlim ReadStarted => readStarted;

        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
            readStarted.Set();
            throw new IOException("Expected test failure.");
        }

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) { }
        public bool DeleteChunk(ChunkStorageKey key) => false;
        public void Flush() { }
        public void Dispose() => readStarted.Dispose();
    }

    sealed class BenchmarkIdentity : IServerIdentity {
        public string AddAssertion(string sdp) => sdp;
        public void Dispose() { }
    }
}
