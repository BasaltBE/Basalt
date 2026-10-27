using System.Diagnostics;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using Xunit;
using Xunit.Abstractions;

namespace BasaltBE.Basalt.Tests;

public sealed class PendingTickStorageTests(ITestOutputHelper output) {
    [Fact]
    public void BenchmarkPendingTickDecodeWithVanillaSizedList() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        Chunk source = new(0, 0) { Dimension = dimension };
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 1000 });
        ListTag records = new() { ElementType = BasaltBE.Nbt.Enums.TagType.Compound };
        pending.Add("tickList", records);
        CompoundTag blockState = BlockStorage.CreatePermutationTag(BlockPalette.FindDefaultPermutation("minecraft:water")!);
        for (int index = 0; index < 1642; index++) {
            CompoundTag record = new();
            record.Add("blockState", blockState);
            record.Add("time", new LongTag { Value = 2000 + index });
            record.Add("x", new IntTag { Value = index & 15 });
            record.Add("y", new IntTag { Value = 0 });
            record.Add("z", new IntTag { Value = (index >> 4) & 15 });
            records.Value.Add(record);
        }
        source.PendingTicks = pending;
        ChunkStorageEntry[] entries = source.Serialize();
        const int warmupCount = 10;
        const int sampleCount = 100;
        double[] samples = new double[sampleCount];
        long allocated = 0;
        Chunk? decoded = null;

        for (int sample = -warmupCount; sample < sampleCount; sample++) {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            decoded = Chunk.Deserialize(0, 0, entries, dimension);
            long end = Stopwatch.GetTimestamp();
            if (sample >= 0) {
                samples[sample] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
                allocated += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            }
        }

        Assert.Equal(1642, decoded!.PendingTicks!.Get<ListTag>("tickList")!.Value.Count);
        Array.Sort(samples);
        output.WriteLine(
            $"Pending tick decode, 1,642 unknown records, {Assert.Single(entries, entry => entry.Type == ChunkStorageType.PendingTicks).Data.Length} NBT bytes, " +
            $"10 warmups, 100 samples: median {samples[49]:F3} ms, p95 {samples[94]:F3} ms, " +
            $"p99 {samples[98]:F3} ms, max {samples[99]:F3} ms, " +
            $"allocation per decode {allocated / (double)sampleCount / 1024:F1} KiB."
        );
    }

    [Fact]
    public void ChunkPreservesPendingTickNbtAndUnknownFields() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 120 });
        pending.Add("extension", new StringTag { Value = "preserve" });
        CompoundTag entry = new();
        entry.Add("time", new LongTag { Value = 123 });
        entry.Add("unknown", new IntTag { Value = 42 });
        pending.Add("tickList", new ListTag { Value = [entry] });
        Chunk chunk = new(0, 0) { Dimension = dimension, PendingTicks = pending };

        Chunk loaded = Chunk.Deserialize(0, 0, chunk.Serialize(), dimension);
        Assert.False(loaded.Empty);
        Assert.Equal("preserve", loaded.PendingTicks!.Get<StringTag>("extension")!.Value);
        ListTag ticks = loaded.PendingTicks.Get<ListTag>("tickList")!;
        CompoundTag loadedEntry = Assert.IsType<CompoundTag>(Assert.Single(ticks.Value));
        Assert.Equal(42, loadedEntry.Get<IntTag>("unknown")!.Value);
        Assert.Equal(123, loadedEntry.Get<LongTag>("time")!.Value);
        Assert.Equal(
            Assert.Single(chunk.Serialize(), item => item.Type == ChunkStorageType.PendingTicks).Data,
            Assert.Single(loaded.Serialize(), item => item.Type == ChunkStorageType.PendingTicks).Data
        );
    }

    [Fact]
    public void PendingTickDecodeRejectsExcessiveNesting() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        CompoundTag nested = new();
        for (int depth = 0; depth < 40; depth++) {
            CompoundTag parent = new();
            parent.Add("child", nested);
            nested = parent;
        }
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 0 });
        pending.Add("tickList", new ListTag());
        pending.Add("unknown", nested);
        Chunk chunk = new(0, 0) { Dimension = dimension, PendingTicks = pending };
        ChunkStorageEntry[] entries = chunk.Serialize();

        Assert.Throws<InvalidDataException>(() => Chunk.Deserialize(0, 0, entries, dimension));
    }

    [Fact]
    public void UnsupportedPendingRecordKeepsItsRemainingDelayAcrossAreaPause() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        CompoundTag record = new();
        record.Add("blockState", BlockStorage.CreatePermutationTag(BlockPalette.FindDefaultPermutation("minecraft:water")!));
        record.Add("time", new LongTag { Value = 110 });
        record.Add("x", new IntTag { Value = 1 });
        record.Add("y", new IntTag { Value = 0 });
        record.Add("z", new IntTag { Value = 1 });
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 100 });
        pending.Add("tickList", new ListTag {
            ElementType = BasaltBE.Nbt.Enums.TagType.Compound,
            Value = [record]
        });
        chunk.PendingTicks = pending;

        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        for (int tick = 0; tick < 3; tick++)
            dimension.Tick();
        dimension.RemoveTickingArea("test");
        dimension.PrepareScheduledTicks(chunk);

        Assert.Equal(103, pending.Get<IntTag>("currentTick")!.Value);
        Assert.Equal(110, record.Get<LongTag>("time")!.Value);
    }

    [Fact]
    public void UnsupportedPendingRecordMarksItsActiveClockDirtyForCheckpoint() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        CompoundTag record = new();
        record.Add("blockState", BlockStorage.CreatePermutationTag(BlockPalette.FindDefaultPermutation("minecraft:water")!));
        record.Add("time", new LongTag { Value = 140 });
        record.Add("x", new IntTag { Value = 1 });
        record.Add("y", new IntTag { Value = 0 });
        record.Add("z", new IntTag { Value = 1 });
        CompoundTag pending = new();
        pending.Add("currentTick", new IntTag { Value = 100 });
        pending.Add("tickList", new ListTag {
            ElementType = BasaltBE.Nbt.Enums.TagType.Compound,
            Value = [record]
        });
        chunk.PendingTicks = pending;
        dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        long revision = chunk.Revision;

        for (int tick = 0; tick < 20; tick++)
            dimension.Tick();
        dimension.PrepareScheduledTicks(chunk);

        Assert.True(chunk.Revision > revision);
        Assert.Equal(120, pending.Get<IntTag>("currentTick")!.Value);
        Assert.Equal(140, record.Get<LongTag>("time")!.Value);
    }

    [Fact]
    public void ChunkRejectsPendingTicksLargerThanTheRemainingRecordBudget() {
        using Dimension dimension = new("minecraft:overworld", new VoidGenerator());
        Chunk chunk = new(0, 0) { Dimension = dimension };
        CompoundTag pending = new();
        pending.Add("payload", new ByteArrayTag { Value = new byte[16 * 1024 * 1024] });
        chunk.PendingTicks = pending;

        Assert.Throws<InvalidDataException>(() => chunk.Serialize());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LevelDbPreservesAndDeletesPendingTickRecords(bool primaryDimension) {
        string path = Path.Combine(Path.GetTempPath(), "basalt-pending-ticks-" + Guid.NewGuid().ToString("N"));
        ChunkStorageKey key = new(DimensionType.Overworld, "test:dimension", primaryDimension, -3, 7);
        byte[] first = [1, 2, 3];
        byte[] replacement = [4, 5];
        try {
            using (LevelDbProvider provider = new(path)) {
                provider.WriteChunks([new(key, [new(ChunkStorageType.PendingTicks, 0, first)])]);
            }

            using (LevelDbProvider provider = new(path)) {
                Assert.Equal(first, Assert.Single(provider.ReadChunk(key)!, entry => entry.Type == ChunkStorageType.PendingTicks).Data);
                provider.WriteChunks([new(key, [new(ChunkStorageType.PendingTicks, 0, replacement)])]);
                Assert.Equal(replacement, Assert.Single(provider.ReadChunk(key)!, entry => entry.Type == ChunkStorageType.PendingTicks).Data);
                provider.WriteChunks([new(key, [])]);
                Assert.DoesNotContain(provider.ReadChunk(key)!, entry => entry.Type == ChunkStorageType.PendingTicks);
                provider.WriteChunks([new(key, [new(ChunkStorageType.PendingTicks, 0, first)])]);
                provider.DeleteChunk(key);
                Assert.Null(provider.ReadChunk(key));
            }
        }
        finally {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void StorageQueueRejectsWritesAtCapacityAndDrainsAcceptedWrites() {
        QueueBoundProvider provider = new();
        using WorldProviderService storage = new(provider);
        using Dimension dimension = new("test:storage-queue", new VoidGenerator());
        ChunkStorageKey key = new(DimensionType.Overworld, "test:storage-queue", true, 0, 0);
        ChunkStorageEntry[] entries = [new(ChunkStorageType.PendingTicks, 0, [1])];
        int accepted = 0;

        try {
            Assert.True(storage.Write(dimension, key, 1, entries));
            Assert.True(provider.WriteStarted.Wait(TimeSpan.FromSeconds(5)));
            accepted++;
            while (storage.Write(dimension, key, accepted + 1, entries))
                accepted++;

            Assert.Equal(4096, storage.PendingOperations);
            Assert.False(storage.Write(dimension, key, accepted + 1, entries));

            provider.ReleaseWrite.Set();
            Assert.True(SpinWait.SpinUntil(() => storage.PendingOperations == 0, TimeSpan.FromSeconds(10)));
            Assert.Equal(accepted, provider.WrittenChunks);
        }
        finally {
            provider.ReleaseWrite.Set();
        }
    }

    [Fact]
    public void StorageQueueRejectsWritesAtByteLimitAndDrainsAcceptedWrites() {
        QueueBoundProvider provider = new();
        using WorldProviderService storage = new(provider);
        using Dimension dimension = new("test:storage-byte-limit", new VoidGenerator());
        ChunkStorageKey key = new(DimensionType.Overworld, "test:storage-byte-limit", true, 0, 0);
        byte[] data = new byte[16 * 1024 * 1024];
        ChunkStorageEntry[] entries = [new(ChunkStorageType.PendingTicks, 0, data)];

        try {
            Assert.True(storage.Write(dimension, key, 1, entries));
            Assert.True(provider.WriteStarted.Wait(TimeSpan.FromSeconds(5)));
            for (int version = 2; version <= 16; version++)
                Assert.True(storage.Write(dimension, key, version, entries));

            Assert.Equal(256L * 1024 * 1024, storage.PendingBytes);
            Assert.False(storage.Write(dimension, key, 17, entries));

            provider.ReleaseWrite.Set();
            Assert.True(SpinWait.SpinUntil(() => storage.PendingOperations == 0, TimeSpan.FromSeconds(10)));
            Assert.Equal(16, provider.WrittenChunks);
            Assert.Equal(0, storage.PendingBytes);
        }
        finally {
            provider.ReleaseWrite.Set();
        }
    }

    sealed class QueueBoundProvider : IWorldProvider {
        readonly ManualResetEventSlim writeStarted = new();
        readonly ManualResetEventSlim releaseWrite = new();
        int writeCalls;
        int writtenChunks;

        public string Identifier => "queue-bound-test";
        public ManualResetEventSlim WriteStarted => writeStarted;
        public ManualResetEventSlim ReleaseWrite => releaseWrite;
        public int WrittenChunks => Volatile.Read(ref writtenChunks);
        public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) => null;

        public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) {
            if (Interlocked.Increment(ref writeCalls) == 1) {
                writeStarted.Set();
                if (!releaseWrite.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The test did not release the blocked storage write.");
            }

            Interlocked.Add(ref writtenChunks, chunks.Length);
        }

        public bool DeleteChunk(ChunkStorageKey key) => false;
        public void Flush() { }
        public void Dispose() {
            writeStarted.Dispose();
            releaseWrite.Dispose();
        }
    }
}
