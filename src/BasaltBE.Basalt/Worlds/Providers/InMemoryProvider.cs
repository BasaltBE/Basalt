using System.Collections.Concurrent;
using BasaltBE.Basalt.Worlds.Providers.Types;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class InMemoryProvider : IWorldProvider {
    private readonly ConcurrentDictionary<ChunkStorageKey, ChunkStorageEntry[]> chunks = new();

    public string Identifier => "memory";

    public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) =>
        chunks.TryGetValue(key, out ChunkStorageEntry[]? entries) ? entries.ToArray() : null;

    public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> records) {
        foreach (ChunkStorageRecord record in records)
            chunks[record.Key] = record.Entries.ToArray();
    }

    public bool DeleteChunk(ChunkStorageKey key) => chunks.TryRemove(key, out _);

    public void Flush() { }

    public void Dispose() { }
}
