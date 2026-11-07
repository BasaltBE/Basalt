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
    const int SaveGroupLimit = 1024;
    const int MaximumStorageCompletionsPerTick = 256;
    readonly Dictionary<long, long> storageRequests = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, long> dirtyChunks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, long> savingChunks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, ulong> saveRetryTicks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, ChunkSaveGroup> saveGroups = [];
    readonly Dictionary<long, HashSet<long>> saveGroupsByChunk = new(ChunkKeyComparer.Instance);
    readonly Queue<long> dirtyQueue = new();
    readonly HashSet<long> queuedDirtyChunks = new(ChunkKeyComparer.Instance);
    internal readonly ConcurrentQueue<WorldStorageCompletion> CompletedStorage = new();
    long nextStorageRequestId;
    long nextSaveGroupId;
    long snapshotGroupId;

    sealed class ChunkSaveGroup(long id, long[] keys) {
        internal readonly long Id = id;
        internal readonly HashSet<long> Keys = [.. keys];
        internal long[]? SnapshotKeys;
        internal long[]? SnapshotRevisions;
        internal long[]? PendingKeys;
        internal long[]? PendingRevisions;
        internal ChunkStorageRecord[]? PendingRecords;
        internal int PendingRecordCount;
        internal int PendingBytes;
        internal ulong RetryTick;
        internal int Failures;
        internal bool InFlight;
        internal bool Failed;
    }

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
                case WorldStorageOperation.WriteEntities:
                    CompleteEntityWrite(completion);
                    break;
                case WorldStorageOperation.Write:
                    CompleteChunkWrite(key, completion);
                    break;
                case WorldStorageOperation.WriteGroup:
                    CompleteSaveGroup(completion);
                    break;
                case WorldStorageOperation.Read:
                    int retainedBytes = CompleteChunkRead(key, completion);
                    World?.Storage?.ReleaseRead(completion.DataBytes - retainedBytes);
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

    void CompleteSaveGroup(WorldStorageCompletion completion) {
        if (!saveGroups.TryGetValue(completion.GroupId, out ChunkSaveGroup? group) ||
            !group.InFlight || completion.GroupKeys is not { } keys ||
            completion.GroupVersions is not { } versions || keys.Length != versions.Length ||
            group.SnapshotKeys is not { } snapshotKeys ||
            group.SnapshotRevisions is not { } snapshotRevisions ||
            snapshotKeys.Length != keys.Length || snapshotRevisions.Length != versions.Length)
            return;

        for (int index = 0; index < keys.Length; index++) {
            long key = GetKey(keys[index].X, keys[index].Z);
            if (savingChunks.TryGetValue(key, out long revision) && revision == versions[index])
                savingChunks.Remove(key);
        }

        group.InFlight = false;
        if (completion.Error is not null) {
            group.SnapshotKeys = null;
            group.SnapshotRevisions = null;
            group.Failures++;
            if (group.Failures >= 3) {
                Logger.Scoped("Storage").Error(
                    $"Could not save piston chunk group {group.Id} in {Identifier} after {group.Failures} attempts: {completion.Error.Message}"
                );
                group.Failed = true;
                return;
            }
            group.RetryTick = TickValue + 20;
            return;
        }

        group.Failures = 0;
        group.RetryTick = 0;
        for (int index = 0; index < snapshotKeys.Length; index++) {
            if (dirtyChunks.TryGetValue(snapshotKeys[index], out long revision) && revision == snapshotRevisions[index])
                dirtyChunks.Remove(snapshotKeys[index]);
        }
        group.SnapshotKeys = null;
        group.SnapshotRevisions = null;

        foreach (ChunkSaveGroup pending in saveGroups.Values.ToArray()) {
            if (pending.InFlight || pending.Keys.Any(dirtyChunks.ContainsKey))
                continue;
            RemoveSaveGroup(pending);
        }
    }

    int CompleteChunkRead(long key, WorldStorageCompletion completion) {
        if (!storageRequests.TryGetValue(key, out long requestId) || requestId != completion.RequestId)
            return 0;

        storageRequests.Remove(key);
        if (!chunks.TryGetValue(key, out Chunk? current) || current is not null) {
            pendingRequestCount--;
            return 0;
        }

        if (completion.Error is not null) {
            chunks.Remove(key);
            pendingRequestCount--;
            return 0;
        }

        int retainedBytes = 0;
        if (completion.Entities is { Length: > 0 } actors) {
            foreach (EntityStorageRecord actor in actors)
                retainedBytes = checked(retainedBytes + actor.Data.Length);
            entityLoads.Enqueue(new EntityLoad(completion.Key, actors));
        }

        if (completion.Chunk is { } chunk) {
            chunk.Dimension = this;
            chunks[key] = chunk;
            chunkCount++;
            pendingRequestCount--;
            LoadScheduledTicks(chunk);
            Redstone.ChunkLoaded(this, chunk);
            return retainedBytes;
        }

        if (!generationService.Request(this, Generator, key, completion.Key.X, completion.Key.Z)) {
            chunks.Remove(key);
            pendingRequestCount--;
        }
        return retainedBytes;
    }

    internal void MarkChunkDirty(Chunk chunk) {
        if (World?.Storage is null)
            return;

        long key = GetKey(chunk.X, chunk.Z);
        dirtyChunks[key] = chunk.Revision;
        if (queuedDirtyChunks.Add(key))
            dirtyQueue.Enqueue(key);
    }

    internal bool GroupChunkSaves(ReadOnlySpan<long> keys) {
        if (World?.Storage is null)
            return true;
        if (keys.IsEmpty || keys.Length > 16)
            return false;

        HashSet<long> uniqueKeys = [];
        foreach (long key in keys) {
            if (!uniqueKeys.Add(key))
                continue;
            if (!chunks.TryGetValue(key, out Chunk? chunk) || chunk is null)
                return false;
        }
        if (uniqueKeys.Count > 16)
            return false;

        HashSet<long> groupIds = [];
        bool addedGroup;
        do {
            addedGroup = false;
            foreach (long key in uniqueKeys.ToArray()) {
                if (!saveGroupsByChunk.TryGetValue(key, out HashSet<long>? overlappingGroups))
                    continue;

                foreach (long groupId in overlappingGroups) {
                    if (groupIds.Contains(groupId) || !saveGroups.TryGetValue(groupId, out ChunkSaveGroup? overlapping) || overlapping.InFlight)
                        continue;
                    groupIds.Add(groupId);
                    uniqueKeys.UnionWith(overlapping.Keys);
                    addedGroup = true;
                    if (uniqueKeys.Count > 16)
                        return false;
                }
            }
        } while (addedGroup);

        if (saveGroups.Count - groupIds.Count >= SaveGroupLimit)
            return false;

        foreach (long groupId in groupIds)
            if (saveGroups.TryGetValue(groupId, out ChunkSaveGroup? overlapping))
                RemoveSaveGroup(overlapping);

        ChunkSaveGroup group = new(++nextSaveGroupId, [.. uniqueKeys]);
        saveGroups.Add(group.Id, group);
        foreach (long key in group.Keys) {
            if (!saveGroupsByChunk.TryGetValue(key, out HashSet<long>? memberships)) {
                memberships = [];
                saveGroupsByChunk.Add(key, memberships);
            }
            memberships.Add(group.Id);
        }
        return true;
    }

    void TickStorageSaves() {
        if (World?.Storage is not { } storage)
            return;

        long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * ChunkStorageSaveBudgetMilliseconds / 1000;
        if (snapshotGroupId != 0 && saveGroups.TryGetValue(snapshotGroupId, out ChunkSaveGroup? activeGroup)) {
            QueueSaveGroup(storage, activeGroup, deadline);
        }
        int eligibleGroups = 0;
        foreach (ChunkSaveGroup group in saveGroups.Values) {
            if (eligibleGroups >= MaximumChunkSavesPerTick || Stopwatch.GetTimestamp() >= deadline)
                break;
            if (group.Id == snapshotGroupId || group.InFlight || group.Failed ||
                TickValue < group.RetryTick || group.Keys.Any(savingChunks.ContainsKey))
                continue;

            QueueSaveGroup(storage, group, deadline);
            eligibleGroups++;
            if (snapshotGroupId != 0)
                break;
        }

        if (TickValue % ChunkStorageSaveIntervalTicks != 0)
            return;

        int remaining = Math.Min(dirtyQueue.Count, MaximumChunkSavesPerTick);
        while (remaining-- > 0 && Stopwatch.GetTimestamp() < deadline) {
            long key = dirtyQueue.Dequeue();
            queuedDirtyChunks.Remove(key);
            if (saveGroupsByChunk.ContainsKey(key))
                continue;
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

    internal bool FlushStorage() {
        if (World?.Storage is not { } storage)
            return true;

        foreach ((long key, ScheduledChunk state) in scheduledChunks) {
            if ((state.Ticks.Count > 0 || state.HasUnindexedRecords) &&
                chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null)
                chunk.MarkDirty();
        }

        while (saveGroups.Values.Any(static group => !group.Failed)) {
            foreach (ChunkSaveGroup group in saveGroups.Values)
                QueueSaveGroup(storage, group, long.MaxValue, force: true);
            Thread.Sleep(1);
            TickStorageCompletions();
            LoadEntities(flush: true);
        }

        LoadEntities(flush: true);
        QueueEntitySaves();
        while (entitySaveQueue.Count > 0 || inFlightEntities > 0) {
            SaveEntities(flush: true);
            Thread.Sleep(1);
            foreach (Dimension dimension in World.Dimensions.Values) {
                dimension.TickStorageCompletions();
                dimension.LoadEntities(flush: true);
            }
        }
        bool saved = !saveGroups.Values.Any(static group => group.Failed) &&
            !entitySaves.Values.Any(static state => state.Pending);

        foreach ((long key, long revision) in dirtyChunks.ToArray()) {
            if (!chunks.TryGetValue(key, out Chunk? chunk) || chunk is null ||
                saveGroupsByChunk.ContainsKey(key) ||
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
            )) {
                Thread.Sleep(1);
                TickStorageCompletions();
                LoadEntities(flush: true);
            }

            savingChunks[key] = revision;
        }

        return saved;
    }

    void QueueSaveGroup(
        WorldProviderService storage,
        ChunkSaveGroup group,
        long deadline = long.MaxValue,
        bool force = false
    ) {
        if (group.InFlight || group.Failed || !force && TickValue < group.RetryTick ||
            group.Keys.Any(savingChunks.ContainsKey) ||
            snapshotGroupId != 0 && snapshotGroupId != group.Id)
            return;

        snapshotGroupId = group.Id;
        if (group.PendingKeys is null) {
            group.PendingKeys = [.. group.Keys.Order()];
            group.PendingRevisions = new long[group.PendingKeys.Length];
            group.PendingRecords = new ChunkStorageRecord[group.PendingKeys.Length];
        }

        long[] pendingKeys = group.PendingKeys;
        long[] pendingRevisions = group.PendingRevisions!;
        ChunkStorageRecord[] pendingRecords = group.PendingRecords!;
        for (int index = 0; index < group.PendingRecordCount; index++) {
            if (!chunks.TryGetValue(pendingKeys[index], out Chunk? chunk) || chunk is null) {
                FailSaveGroup(group, $"Chunk {pendingKeys[index]} is not loaded.");
                return;
            }
            if (chunk.Revision == pendingRevisions[index])
                continue;

            group.PendingKeys = null;
            group.PendingRevisions = null;
            group.PendingRecords = null;
            group.PendingRecordCount = 0;
            group.PendingBytes = 0;
            return;
        }

        while (group.PendingRecordCount < pendingKeys.Length &&
            (force || Stopwatch.GetTimestamp() < deadline)) {
            int index = group.PendingRecordCount;
            long key = pendingKeys[index];
            if (!chunks.TryGetValue(key, out Chunk? chunk) || chunk is null) {
                FailSaveGroup(group, $"Chunk {key} is not loaded.");
                return;
            }

            PrepareScheduledTicks(chunk);
            if (SerializeChunk(chunk) is not { } entries) {
                FailSaveGroup(group, $"Chunk ({chunk.X}, {chunk.Z}) could not be serialized.");
                return;
            }
            int chunkBytes = 0;
            foreach (ChunkStorageEntry entry in entries)
                chunkBytes = checked(chunkBytes + entry.Data.Length);
            group.PendingBytes = checked(group.PendingBytes + chunkBytes);
            if (chunkBytes > WorldProviderService.MaximumChunkDataBytes ||
                group.PendingBytes > WorldProviderService.MaximumPendingBytes) {
                FailSaveGroup(group, $"Chunk group {group.Id} exceeds the storage queue byte limit.");
                return;
            }

            pendingRecords[index] = new ChunkStorageRecord(
                new ChunkStorageKey(Type, Identifier, PrimaryStorageDimension, chunk.X, chunk.Z),
                entries
            );
            pendingRevisions[index] = chunk.Revision;
            group.PendingRecordCount++;
        }

        if (group.PendingRecordCount != pendingKeys.Length)
            return;

        if (!storage.WriteGroup(this, group.Id, pendingRecords, pendingRevisions, group.PendingBytes))
            return;

        group.SnapshotKeys = pendingKeys;
        group.SnapshotRevisions = pendingRevisions;
        group.InFlight = true;
        group.PendingKeys = null;
        group.PendingRevisions = null;
        group.PendingRecords = null;
        group.PendingRecordCount = 0;
        group.PendingBytes = 0;
        snapshotGroupId = 0;
        for (int index = 0; index < pendingKeys.Length; index++)
            savingChunks[pendingKeys[index]] = pendingRevisions[index];
    }

    void FailSaveGroup(ChunkSaveGroup group, string reason) {
        group.Failed = true;
        group.PendingKeys = null;
        group.PendingRevisions = null;
        group.PendingRecords = null;
        group.PendingRecordCount = 0;
        group.PendingBytes = 0;
        if (snapshotGroupId == group.Id)
            snapshotGroupId = 0;
        Logger.Scoped("Storage").Error($"Could not save piston chunk group {group.Id} in {Identifier}: {reason}");
    }

    void RemoveSaveGroup(ChunkSaveGroup group) {
        saveGroups.Remove(group.Id);
        if (snapshotGroupId == group.Id)
            snapshotGroupId = 0;
        foreach (long key in group.Keys) {
            if (!saveGroupsByChunk.TryGetValue(key, out HashSet<long>? groupIds))
                continue;
            groupIds.Remove(group.Id);
            if (groupIds.Count == 0)
                saveGroupsByChunk.Remove(key);
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
