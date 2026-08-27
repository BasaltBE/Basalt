namespace Basalt.Tests;

using Basalt.Core.Blocks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;

public sealed class DimensionChunkRequestTests {
    [Fact]
    public async Task ConcurrentRequestsForOneChunkShareTheLoad() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        int callbacks = 0;

        Task first = Task.Run(() => dimension.RequestChunks(
            [(0, 0)],
            _ => Interlocked.Increment(ref callbacks)));

        Assert.True(provider.LoadStarted.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, dimension.PendingChunkRequestCount);
        Assert.Equal(1, dimension.RequestChunks(
            [(0, 0)],
            _ => Interlocked.Increment(ref callbacks)));

        provider.ReleaseLoad.Set();
        await first.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, callbacks);
        dimension.Tick(1, 1);
        Assert.Equal(2, callbacks);
        Assert.Equal(0, dimension.PendingChunkRequestCount);
    }

    [Fact]
    public void LoadedChunkLookupDoesNotLoadChunk() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());

        Assert.False(dimension.TryGetLoadedChunk(0, 0, out Chunk? chunk));
        Assert.Null(chunk);
        Assert.False(provider.LoadStarted.IsSet);
    }

    [Fact]
    public void LoadedChunkReadDoesNotLoadChunk() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());

        Assert.Null(dimension.GetLoadedChunk(0, 0));
        Assert.False(provider.LoadStarted.IsSet);
    }

    [Fact]
    public void LoadedPermutationLookupDoesNotLoadChunk() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());

        Assert.False(dimension.TryGetLoadedPermutation(0, 80, 0, out BlockPermutation? permutation));
        Assert.Null(permutation);
        Assert.False(provider.LoadStarted.IsSet);
    }

    [Fact]
    public void BlockLookupDoesNotLoadChunk() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());

        Assert.Null(dimension.GetBlock(0, 80, 0));
        Assert.False(provider.LoadStarted.IsSet);
    }

    [Fact]
    public void LoadedPermutationOrAirDoesNotLoadChunk() {
        using BlockingLoadProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());

        BlockPermutation permutation = dimension.GetLoadedPermutationOrAir(0, 80, 0);

        Assert.True(permutation.Type.Air);
        Assert.False(provider.LoadStarted.IsSet);
    }

    private sealed class BlockingLoadProvider : WorldProvider {
        public ManualResetEventSlim LoadStarted { get; } = new();
        public ManualResetEventSlim ReleaseLoad { get; } = new();

        public override string Identifier => "test";

        public override bool HasChunk(DimensionId dimensionType, int x, int z) => false;

        public override Chunk? LoadChunk(DimensionId dimensionType, int x, int z) {
            LoadStarted.Set();
            ReleaseLoad.Wait(TimeSpan.FromSeconds(5));
            return null;
        }

        public override void SaveChunk(Chunk chunk) { }

        public override void DeleteChunk(DimensionId dimensionType, int x, int z) { }

        public override void Dispose() {
            ReleaseLoad.Set();
            LoadStarted.Dispose();
            ReleaseLoad.Dispose();
        }
    }
}
