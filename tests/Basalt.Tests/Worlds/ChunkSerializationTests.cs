namespace Basalt.Tests;

using Basalt.Core.Tasks;
using Basalt.Core.Blocks;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions;

public sealed class ChunkSerializationTests {
    [Fact]
    public void ChunkEmptyTracksBlockState() {
        Chunk chunk = new(0, 0, DimensionId.Overworld);
        BlockPermutation stone = BlockPermutation.Resolve("minecraft:stone");
        BlockPermutation air = BlockPermutation.Resolve("minecraft:air");

        Assert.True(chunk.Empty);
        Assert.True(chunk.IsEmpty());

        chunk.SetPermutation(0, 0, 0, stone);
        Assert.False(chunk.Empty);
        Assert.False(chunk.IsEmpty());

        chunk.SetPermutation(0, 0, 0, air);
        Assert.True(chunk.Empty);
    }

    [Fact]
    public void ChunkEmptyIgnoresUnusedPaletteEntries() {
        BlockStorage storage = new(
            [BlockStorage.Air, BlockPermutation.Resolve("minecraft:stone").NetworkId],
            new int[BlockStorage.MaxSize]);
        SubChunk subChunk = new(layers: [storage]);
        Chunk chunk = new(0, 0, DimensionId.Overworld, [subChunk]);

        Assert.True(chunk.Empty);
        Assert.True(storage.IsEmpty());
    }

    [Fact]
    public void ChunkSerializationCompletesInTheOwnerMailbox() {
        using TaskWorkerPool pool = new(1);
        ExecutionDomainMailbox mailbox = new(2);
        using ManualResetEventSlim completed = new();
        Chunk chunk = new(0, 0, DimensionId.Overworld);
        ChunkSerializationTask task = new(chunk, (_, payload, error) => {
            Assert.Null(error);
            Assert.NotNull(payload);
            completed.Set();
        }) {
            CompletionMailbox = mailbox
        };

        Assert.True(pool.TryEnqueue(task));
        Assert.False(completed.Wait(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(1, mailbox.Drain(1, _ => Assert.Fail("The serialization completion failed.")));
        Assert.True(completed.IsSet);
    }
}
