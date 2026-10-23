using System.Collections.Concurrent;
using System.Diagnostics;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Worlds.Providers;

internal sealed class WorldProviderService : IDisposable {
    private const int MaximumPendingOperations = 4096;
    private const long MaximumPendingBytes = 256L * 1024 * 1024;
    private const int MaximumWriteBatch = 64;
    internal const int MaximumChunkDataBytes = 16 * 1024 * 1024;
    private readonly IWorldProvider provider;
    private readonly BlockingCollection<StorageRequest> requests = new(new ConcurrentQueue<StorageRequest>(), MaximumPendingOperations);
    private readonly ChunkStorageRecord[] writeBatch = new ChunkStorageRecord[MaximumWriteBatch];
    private readonly StorageRequest[] batchRequests = new StorageRequest[MaximumWriteBatch];
    private readonly Thread worker;
    private long pendingBytes;
    private int pendingOperations;
    private int disposed;
    private long readCount;
    private long writeCount;
    private long readTicks;
    private long decodeCount;
    private long decodeTicks;
    private long writeTicks;

    internal WorldProviderService(IWorldProvider provider) {
        this.provider = provider;
        worker = new Thread(Work) {
            IsBackground = true,
            Name = $"Basalt storage {provider.Identifier}"
        };
        worker.Start();
    }

    internal int PendingOperations => Volatile.Read(ref pendingOperations);
    internal long PendingBytes => Interlocked.Read(ref pendingBytes);
    internal long ReadCount => Interlocked.Read(ref readCount);
    internal long WriteCount => Interlocked.Read(ref writeCount);
    internal double AverageReadMilliseconds => GetAverageMilliseconds(
        Interlocked.Read(ref readTicks),
        Interlocked.Read(ref readCount)
    );
    internal double AverageWriteMilliseconds => GetAverageMilliseconds(
        Interlocked.Read(ref writeTicks),
        Interlocked.Read(ref writeCount)
    );
    internal double AverageDecodeMilliseconds => GetAverageMilliseconds(
        Interlocked.Read(ref decodeTicks),
        Interlocked.Read(ref decodeCount)
    );
    internal WorldStorageMetrics Metrics => new(
        true,
        PendingOperations,
        PendingBytes,
        ReadCount,
        WriteCount,
        AverageReadMilliseconds,
        AverageDecodeMilliseconds,
        AverageWriteMilliseconds
    );

    internal void ReleaseRead(int length) => Interlocked.Add(ref pendingBytes, -length);

    internal bool Read(Dimension dimension, ChunkStorageKey key, long requestId) => Add(new StorageRequest(
        dimension,
        WorldStorageOperation.Read,
        key,
        requestId,
        0,
        null,
        0
    ));

    internal bool Write(Dimension dimension, ChunkStorageKey key, long version, ChunkStorageEntry[] entries) {
        ArgumentNullException.ThrowIfNull(entries);
        int dataBytes = 0;
        foreach (ChunkStorageEntry entry in entries)
            dataBytes = checked(dataBytes + entry.Data.Length);
        if (dataBytes > MaximumChunkDataBytes)
            return false;
        if (Interlocked.Add(ref pendingBytes, dataBytes) > MaximumPendingBytes) {
            Interlocked.Add(ref pendingBytes, -dataBytes);
            return false;
        }

        if (Add(new StorageRequest(dimension, WorldStorageOperation.Write, key, 0, version, entries, dataBytes)))
            return true;

        Interlocked.Add(ref pendingBytes, -dataBytes);
        return false;
    }

    internal bool Delete(Dimension dimension, ChunkStorageKey key, long requestId) => Add(new StorageRequest(
        dimension,
        WorldStorageOperation.Delete,
        key,
        requestId,
        0,
        null,
        0
    ));

    private bool Add(StorageRequest request) {
        if (Volatile.Read(ref disposed) != 0)
            return false;

        if (Interlocked.Increment(ref pendingOperations) > MaximumPendingOperations) {
            Interlocked.Decrement(ref pendingOperations);
            return false;
        }

        try {
            if (requests.TryAdd(request))
                return true;
        }
        catch (ObjectDisposedException) {
        }
        catch (InvalidOperationException) when (Volatile.Read(ref disposed) != 0) {
        }

        Interlocked.Decrement(ref pendingOperations);
        return false;
    }

    private void Work() {
        StorageRequest? deferred = null;
        foreach (StorageRequest request in requests.GetConsumingEnumerable()) {
            if (deferred is { } next) {
                deferred = null;
                Process(next);
            }

            if (request.Operation != WorldStorageOperation.Write) {
                Process(request);
                continue;
            }

            int count = 0;
            AddWrite(request, ref count);
            while (count < MaximumWriteBatch && requests.TryTake(out StorageRequest following)) {
                if (following.Operation != WorldStorageOperation.Write) {
                    deferred = following;
                    break;
                }

                AddWrite(following, ref count);
            }

            WriteBatch(count);
        }

        if (deferred is { } remaining)
            Process(remaining);

        try {
            provider.Flush();
        }
        catch (Exception error) {
            Logger.Scoped("Storage").Error($"Could not flush provider {provider.Identifier}: {error.Message}");
        }
        try {
            provider.Dispose();
        }
        catch (Exception error) {
            Logger.Scoped("Storage").Error($"Could not dispose provider {provider.Identifier}: {error.Message}");
        }
    }

    private void AddWrite(StorageRequest request, ref int count) {
        batchRequests[count] = request;
        writeBatch[count] = new ChunkStorageRecord(request.Key, request.Entries!);
        count++;
    }

    private void WriteBatch(int count) {
        long start = Stopwatch.GetTimestamp();
        Exception? error = null;
        try {
            provider.WriteChunks(writeBatch.AsSpan(0, count));
        }
        catch (Exception exception) {
            error = exception;
        }
        finally {
            Interlocked.Add(ref writeTicks, Stopwatch.GetTimestamp() - start);
            Interlocked.Add(ref writeCount, count);
        }

        if (error is not null)
            Logger.Scoped("Storage").Error(
                $"Provider {provider.Identifier} could not write {count} chunks: {error.Message}"
            );

        for (int index = 0; index < count; index++) {
            StorageRequest request = batchRequests[index];
            request.Dimension.CompletedStorage.Enqueue(new WorldStorageCompletion(
                request.Operation,
                request.Key,
                request.RequestId,
                request.Version,
                null,
                0,
                false,
                error
            ));
            Interlocked.Add(ref pendingBytes, -request.DataBytes);
            Interlocked.Decrement(ref pendingOperations);
            batchRequests[index] = default;
            writeBatch[index] = default;
        }
    }

    private void Process(StorageRequest request) {
        Chunk? chunk = null;
        int dataBytes = 0;
        bool deleted = false;
        Exception? error = null;
        try {
            switch (request.Operation) {
                case WorldStorageOperation.Read:
                    long readStart = Stopwatch.GetTimestamp();
                    ChunkStorageEntry[]? entries = provider.ReadChunk(request.Key);
                    Interlocked.Add(ref readTicks, Stopwatch.GetTimestamp() - readStart);
                    int entryBytes = 0;
                    if (entries is not null) {
                        foreach (ChunkStorageEntry entry in entries)
                            entryBytes = checked(entryBytes + entry.Data.Length);
                    }
                    if (entryBytes > MaximumChunkDataBytes) {
                        entries = null;
                        error = new InvalidDataException("Stored chunk exceeds the maximum size.");
                    }
                    else if (entries is not null &&
                        Interlocked.Add(ref pendingBytes, entryBytes) > MaximumPendingBytes) {
                        Interlocked.Add(ref pendingBytes, -entryBytes);
                        entries = null;
                        error = new InvalidDataException("Storage completion queue is full.");
                    }
                    else if (entries is not null) {
                        dataBytes = entryBytes;
                        long decodeStart = Stopwatch.GetTimestamp();
                        try {
                            chunk = Chunk.Deserialize(request.Key.X, request.Key.Z, entries);
                        }
                        catch (Exception exception) {
                            Interlocked.Add(ref pendingBytes, -dataBytes);
                            dataBytes = 0;
                            error = exception;
                        }
                        finally {
                            Interlocked.Add(ref decodeTicks, Stopwatch.GetTimestamp() - decodeStart);
                            Interlocked.Increment(ref decodeCount);
                        }
                    }
                    Interlocked.Increment(ref readCount);
                    break;
                case WorldStorageOperation.Delete:
                    deleted = provider.DeleteChunk(request.Key);
                    break;
            }
        }
        catch (Exception exception) {
            error = exception;
        }

        if (error is not null)
            Logger.Scoped("Storage").Error(
                $"Provider {provider.Identifier} could not {request.Operation.ToString().ToLowerInvariant()} " +
                $"chunk {request.Key.DimensionIdentifier} ({request.Key.X}, {request.Key.Z}): {error.Message}"
            );

        request.Dimension.CompletedStorage.Enqueue(new WorldStorageCompletion(
            request.Operation,
            request.Key,
            request.RequestId,
            request.Version,
            chunk,
            dataBytes,
            deleted,
            error
        ));
        Interlocked.Decrement(ref pendingOperations);
    }

    private static double GetAverageMilliseconds(long ticks, long count) =>
        count == 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency / count;

    public void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        requests.CompleteAdding();
        worker.Join();
        requests.Dispose();
    }

    private readonly record struct StorageRequest(
        Dimension Dimension,
        WorldStorageOperation Operation,
        ChunkStorageKey Key,
        long RequestId,
        long Version,
        ChunkStorageEntry[]? Entries,
        int DataBytes
    );
}
