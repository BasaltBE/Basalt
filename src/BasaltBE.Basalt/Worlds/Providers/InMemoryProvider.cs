using BasaltBE.Basalt.Worlds.Providers.Types;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class InMemoryProvider : IWorldProvider, IWorldDataProvider, IEntityProvider {
    private readonly Dictionary<ChunkStorageKey, ChunkStorageEntry[]> chunks = [];
    private readonly Lock chunksLock = new();
    private byte[]? worldData;
    private long reservedEntityId;
    private readonly Dictionary<ChunkStorageKey, Dictionary<long, byte[]>> actors = [];

    public string Identifier => "memory";

    public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
        lock (chunksLock)
            return chunks.TryGetValue(key, out ChunkStorageEntry[]? entries) ? entries.ToArray() : null;
    }

    public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> records) {
        lock (chunksLock) {
            foreach (ChunkStorageRecord record in records) chunks[record.Key] = record.Entries.ToArray();
        }
    }

    public bool DeleteChunk(ChunkStorageKey key) {
        lock (chunksLock) {
            actors.Remove(key);
            return chunks.Remove(key);
        }
    }

    public long ReserveEntityIds(long count) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        lock (chunksLock) {
            long lastId = reservedEntityId;
            foreach (var records in actors.Values)
                foreach (long id in records.Keys) lastId = Math.Max(lastId, id);
            reservedEntityId = checked(lastId + count);
            return checked(lastId + 1);
        }
    }

    public EntityStorageRecord[] ReadEntities(ChunkStorageKey key) {
        lock (chunksLock)
            return actors.TryGetValue(key, out var records)
                ? records.Select(static record => new EntityStorageRecord(record.Key, record.Value.ToArray())).ToArray()
                : [];
    }

    public void WriteEntities(ReadOnlySpan<EntityStorageChange> changes) {
        foreach (EntityStorageChange change in changes)
            if ((change.Current is null) != (change.Data is null))
                throw new ArgumentException("Actor data and destination must both be present or absent.", nameof(changes));
        lock (chunksLock) {
            foreach (EntityStorageChange change in changes) {
                if (change.Previous is { } previous && actors.TryGetValue(previous, out var old)) {
                    old.Remove(change.Id);
                    if (old.Count == 0) actors.Remove(previous);
                }
                if (change.Current is not { } current || change.Data is null) continue;
                if (!actors.TryGetValue(current, out var records)) {
                    records = [];
                    actors.Add(current, records);
                }
                records[change.Id] = change.Data.ToArray();
            }
        }
    }

    public byte[]? ReadWorldData() => worldData?.ToArray();

    public void WriteWorldData(ReadOnlySpan<byte> data) => worldData = data.ToArray();

    public void Flush() { }

    public void Dispose() { }
}
