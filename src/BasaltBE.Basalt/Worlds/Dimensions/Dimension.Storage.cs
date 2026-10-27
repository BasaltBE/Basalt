using System.Collections.Concurrent;
using System.Diagnostics;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed partial class Dimension {
    const int ChunkStorageSaveIntervalTicks = 20;
    const int ChunkStorageSaveBudgetMilliseconds = 1;
    const int MaximumChunkSavesPerTick = 64;
    const int MaximumStorageCompletionsPerTick = 256;
    readonly Dictionary<long, long> storageRequests = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, long> dirtyChunks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, long> savingChunks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, ulong> saveRetryTicks = new(ChunkKeyComparer.Instance);
    readonly Queue<long> dirtyQueue = new();
    readonly HashSet<long> queuedDirtyChunks = new(ChunkKeyComparer.Instance);
    internal readonly ConcurrentQueue<WorldStorageCompletion> CompletedStorage = new();
    long nextStorageRequestId;

    bool QueueChunk(int chunkX, int chunkZ, long key) {
        chunks.Add(key, null);
        pendingRequestCount++;
        if (World?.Storage is { } storage) {
            long requestId = ++nextStorageRequestId;
            if (storage.Read(
                this,
                new ChunkStorageKey(Type, Identifier, PrimaryStorageDimension, chunkX, chunkZ),
                requestId
            )) {
                storageRequests.Add(key, requestId);
                return true;
            }
        }
        else if (generationService.Request(this, Generator, key, chunkX, chunkZ)) {
            return true;
        }

        chunks.Remove(key);
        pendingRequestCount--;
        return false;
    }

    void TickStorageCompletions() {
        int processed = 0;
        while (processed < MaximumStorageCompletionsPerTick &&
            CompletedStorage.TryDequeue(out WorldStorageCompletion completion)) {
            processed++;
            long key = GetKey(completion.Key.X, completion.Key.Z);
            switch (completion.Operation) {
                case WorldStorageOperation.Write:
                    CompleteChunkWrite(key, completion);
                    break;
                case WorldStorageOperation.Read:
                    if (completion.DataBytes > 0)
                        World?.Storage?.ReleaseRead(completion.DataBytes);
                    CompleteChunkRead(key, completion);
                    break;
            }
        }
    }

    void CompleteChunkWrite(long key, WorldStorageCompletion completion) {
        if (!savingChunks.Remove(key, out long revision) || revision != completion.Version)
            return;

        if (completion.Error is not null) {
            saveRetryTicks[key] = TickValue + 20;
            if (queuedDirtyChunks.Add(key))
                dirtyQueue.Enqueue(key);
            return;
        }

        saveRetryTicks.Remove(key);
        if (dirtyChunks.TryGetValue(key, out long dirtyRevision) && dirtyRevision == revision)
            dirtyChunks.Remove(key);
        else if (dirtyChunks.ContainsKey(key) && queuedDirtyChunks.Add(key))
            dirtyQueue.Enqueue(key);
    }

    void CompleteChunkRead(long key, WorldStorageCompletion completion) {
        if (!storageRequests.TryGetValue(key, out long requestId) || requestId != completion.RequestId)
            return;

        storageRequests.Remove(key);
        if (!chunks.TryGetValue(key, out Chunk? current) || current is not null) {
            pendingRequestCount--;
            return;
        }

        if (completion.Error is not null) {
            chunks.Remove(key);
            pendingRequestCount--;
            return;
        }

        if (completion.Chunk is { } chunk) {
            chunk.Dimension = this;
            chunks[key] = chunk;
            chunkCount++;
            pendingRequestCount--;
            LoadScheduledTicks(chunk);
            Redstone.ChunkLoaded(this, chunk);
            return;
        }

        if (!generationService.Request(this, Generator, key, completion.Key.X, completion.Key.Z)) {
            chunks.Remove(key);
            pendingRequestCount--;
        }
    }

    internal void MarkChunkDirty(Chunk chunk) {
        if (World?.Storage is null)
            return;

        long key = GetKey(chunk.X, chunk.Z);
        dirtyChunks[key] = chunk.Revision;
        if (queuedDirtyChunks.Add(key))
            dirtyQueue.Enqueue(key);
    }

    void TickStorageSaves() {
        if (World?.Storage is not { } storage || TickValue % ChunkStorageSaveIntervalTicks != 0)
            return;

        long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * ChunkStorageSaveBudgetMilliseconds / 1000;
        int remaining = Math.Min(dirtyQueue.Count, MaximumChunkSavesPerTick);
        while (remaining-- > 0 && Stopwatch.GetTimestamp() < deadline) {
            long key = dirtyQueue.Dequeue();
            queuedDirtyChunks.Remove(key);
            if (!dirtyChunks.TryGetValue(key, out long revision))
                continue;
            if (saveRetryTicks.TryGetValue(key, out ulong retryTick) && TickValue < retryTick) {
                if (queuedDirtyChunks.Add(key))
                    dirtyQueue.Enqueue(key);
                continue;
            }
            if (savingChunks.ContainsKey(key)) {
                if (queuedDirtyChunks.Add(key))
                    dirtyQueue.Enqueue(key);
                continue;
            }
            if (!chunks.TryGetValue(key, out Chunk? chunk) || chunk is null)
                continue;

            PrepareScheduledTicks(chunk);
            if (SerializeChunk(chunk) is not { } entries)
                continue;
            if (!storage.Write(
                this,
                new ChunkStorageKey(Type, Identifier, PrimaryStorageDimension, chunk.X, chunk.Z),
                revision,
                entries
            )) {
                if (queuedDirtyChunks.Add(key))
                    dirtyQueue.Enqueue(key);
                break;
            }

            savingChunks[key] = revision;
        }
    }

    internal void FlushStorage() {
        if (World?.Storage is not { } storage)
            return;

        foreach ((long key, ScheduledChunk state) in scheduledChunks) {
            if ((state.Ticks.Count > 0 || state.HasUnindexedRecords) &&
                chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null)
                chunk.MarkDirty();
        }

        foreach ((long key, long revision) in dirtyChunks) {
            if (!chunks.TryGetValue(key, out Chunk? chunk) || chunk is null ||
                savingChunks.TryGetValue(key, out long savingRevision) && savingRevision == revision)
                continue;
            PrepareScheduledTicks(chunk);
            if (SerializeChunk(chunk) is not { } entries)
                continue;

            while (!storage.Write(
                this,
                new ChunkStorageKey(Type, Identifier, PrimaryStorageDimension, chunk.X, chunk.Z),
                revision,
                entries
            ))
                Thread.Sleep(1);

            savingChunks[key] = revision;
        }
    }

    ChunkStorageEntry[]? SerializeChunk(Chunk chunk) {
        ChunkStorageEntry[] entries;
        try {
            entries = chunk.Serialize();
        }
        catch (Exception error) when (error is InvalidDataException or OverflowException) {
            Logger.Scoped("Storage").Error(
                $"Could not serialize chunk ({chunk.X}, {chunk.Z}) in {Identifier}: {error.Message}"
            );
            return null;
        }

        int dataLength = 0;
        foreach (ChunkStorageEntry entry in entries)
            dataLength = checked(dataLength + entry.Data.Length);

        if (dataLength > WorldProviderService.MaximumChunkDataBytes) {
            Logger.Scoped("Storage").Error(
                $"Chunk ({chunk.X}, {chunk.Z}) in {Identifier} exceeds the storage record limit."
            );
            return null;
        }

        return entries;
    }
}
