using BasaltBE.Basalt.Worlds.Providers.Types;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class InMemoryProvider : IWorldProvider, IWorldDataProvider {
    private readonly Dictionary<ChunkStorageKey, ChunkStorageEntry[]> chunks = [];
    private readonly Lock chunksLock = new();
    private byte[]? worldData;

    public string Identifier => "memory";

    public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
        lock (chunksLock)
            return chunks.TryGetValue(key, out ChunkStorageEntry[]? entries) ? entries.ToArray() : null;
    }

    public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> records) {
        lock (chunksLock) {
            foreach (ChunkStorageRecord record in records)
                chunks[record.Key] = record.Entries.ToArray();
        }
    }

    public bool DeleteChunk(ChunkStorageKey key) {
        lock (chunksLock)
            return chunks.Remove(key);
    }

    public byte[]? ReadWorldData() => worldData?.ToArray();

    public void WriteWorldData(ReadOnlySpan<byte> data) => worldData = data.ToArray();

    public void Flush() { }

    public void Dispose() { }
}
