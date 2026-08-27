namespace Basalt.Tests;

using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;

public sealed class WorldPersistenceTests {
    [Fact]
    public void ChunkSavePublishesPendingSnapshotUntilWriteCompletes() {
        using BlockingProvider provider = new();
        using WorldPersistence persistence = new(provider);
        Chunk chunk = new(3, 4, DimensionId.Overworld) {
            Dirty = true
        };

        persistence.SaveChunk(chunk);

        Assert.True(SpinWait.SpinUntil(
            () => persistence.PendingWorkCount > 0,
            TimeSpan.FromSeconds(5)));
        Assert.True(persistence.ChunkPending(DimensionId.Overworld, 3, 4));
        Assert.NotNull(persistence.GetPendingChunk(DimensionId.Overworld, 3, 4));

        provider.Release();
        persistence.Flush();

        Assert.False(persistence.ChunkPending(DimensionId.Overworld, 3, 4));
        Assert.Null(persistence.GetPendingChunk(DimensionId.Overworld, 3, 4));
    }

    [Fact]
    public void WorldSaveWaitsForQueuedChunkWrites() {
        RecordingProvider provider = new();
        using World world = new("test", provider);
        Dimension dimension = new("overworld", DimensionId.Overworld, provider, new VoidGenerator());
        world.AddDimension(dimension);

        dimension.GetOrCreateChunk(0, 0);
        world.Save();

        Assert.Equal(1, provider.SavedChunks);
    }

    private sealed class BlockingProvider : WorldProvider {
        private readonly ManualResetEventSlim _started = new();
        private readonly ManualResetEventSlim _release = new();

        public override string Identifier => "test";

        public override bool HasChunk(DimensionId dimensionType, int x, int z) => false;

        public override Chunk? LoadChunk(DimensionId dimensionType, int x, int z) => null;

        public override void SaveChunk(Chunk chunk) {
            _started.Set();
            _release.Wait(TimeSpan.FromSeconds(5));
        }

        public override void DeleteChunk(DimensionId dimensionType, int x, int z) { }

        public void Release() {
            Assert.True(_started.Wait(TimeSpan.FromSeconds(5)));
            _release.Set();
        }

        public override void Dispose() {
            _release.Set();
            _started.Dispose();
            _release.Dispose();
        }
    }

    private sealed class RecordingProvider : WorldProvider {
        public int SavedChunks { get; private set; }

        public override string Identifier => "test";

        public override bool HasChunk(DimensionId dimensionType, int x, int z) => false;

        public override Chunk? LoadChunk(DimensionId dimensionType, int x, int z) => null;

        public override void SaveChunk(Chunk chunk) {
            SavedChunks++;
        }

        public override void DeleteChunk(DimensionId dimensionType, int x, int z) { }

        public override void Dispose() { }
    }
}
