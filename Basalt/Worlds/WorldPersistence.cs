namespace Basalt.Core.Worlds;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Types;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

internal sealed class WorldPersistence : IDisposable {
    private readonly WorldProvider _provider;
    private readonly Channel<PersistenceWork> _queue;
    private readonly ConcurrentDictionary<ChunkSaveKey, TaskCompletionSource> _chunkSaves = new();
    private readonly ConcurrentDictionary<ChunkSaveKey, ChunkColumn> _pendingChunks = new();
    private readonly ConcurrentDictionary<string, CompoundTag> _playerSaves = new(StringComparer.Ordinal);
    private readonly Task _worker;
    private int _pendingWorkCount;
    private long _chunkWriteTicks;
    private long _chunkWriteCount;

    public int PendingWorkCount => Volatile.Read(ref _pendingWorkCount);
    public double AverageChunkWriteMilliseconds =>
        Volatile.Read(ref _chunkWriteCount) == 0
            ? 0
            : Volatile.Read(ref _chunkWriteTicks) * 1000.0 /
              Stopwatch.Frequency /
              Volatile.Read(ref _chunkWriteCount);

    public WorldPersistence(WorldProvider provider) {
        _provider = provider;
        _queue = Channel.CreateUnbounded<PersistenceWork>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false
        });
        _worker = Task.Run(Loop);
    }

    public bool ChunkPending(DimensionId type, int x, int z) {
        return _chunkSaves.ContainsKey(new ChunkSaveKey(type, x, z));
    }

    public void SaveChunk(Chunk chunk) {
        ChunkSaveKey key = new(chunk.Type, chunk.X, chunk.Z);
        ChunkColumn snapshot = chunk.CreatePersistenceSnapshot();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _chunkSaves[key] = completion;
        _pendingChunks[key] = snapshot;
        try {
            Write(new ChunkWork(snapshot, key, completion));
        }
        catch {
            _chunkSaves.TryRemove(new KeyValuePair<ChunkSaveKey, TaskCompletionSource>(key, completion));
            _pendingChunks.TryRemove(new KeyValuePair<ChunkSaveKey, ChunkColumn>(key, snapshot));
            throw;
        }
    }

    public void WaitForChunk(DimensionId type, int x, int z) {
        WaitForChunk(new ChunkSaveKey(type, x, z));
    }

    public void SavePlayerData(string xuid, CompoundTag data) {
        _playerSaves[xuid] = data;
        try {
            Write(new PlayerWork(xuid, data));
        }
        catch {
            _playerSaves.TryRemove(new KeyValuePair<string, CompoundTag>(xuid, data));
            throw;
        }
    }

    public void SaveSpawnPosition(DimensionId type, Vec3 position) {
        Write(new SpawnWork(type, position));
    }

    public CompoundTag? GetPendingPlayerData(string xuid) {
        return _playerSaves.TryGetValue(xuid, out CompoundTag? data) ? data : null;
    }

    internal Task GetChunkSaveTask(DimensionId type, int x, int z) {
        return _chunkSaves.TryGetValue(new ChunkSaveKey(type, x, z), out TaskCompletionSource? completion)
            ? completion.Task
            : Task.CompletedTask;
    }

    internal ChunkColumn? GetPendingChunk(DimensionId type, int x, int z) {
        return _pendingChunks.TryGetValue(new ChunkSaveKey(type, x, z), out ChunkColumn? chunk)
            ? chunk
            : null;
    }

    public void Flush() {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Write(new FlushWork(completion));
        completion.Task.GetAwaiter().GetResult();
    }

    public void Dispose() {
        _queue.Writer.TryComplete();
        _worker.GetAwaiter().GetResult();
    }

    private async Task Loop() {
        await foreach (PersistenceWork work in _queue.Reader.ReadAllAsync()) {
            try {
                switch (work) {
                    case ChunkWork chunk:
                        WriteChunk(chunk.Chunk, chunk.Key, chunk.Completion);
                        break;
                    case PlayerWork player:
                        WritePlayerData(player.Xuid, player.Data);
                        break;
                    case SpawnWork spawn:
                        WriteSpawnPosition(spawn);
                        break;
                    case FlushWork flush:
                        flush.Completion.TrySetResult();
                        break;
                }
            }
            finally {
                Interlocked.Decrement(ref _pendingWorkCount);
            }
        }
    }

    private void Write(PersistenceWork work) {
        Interlocked.Increment(ref _pendingWorkCount);
        if (!_queue.Writer.TryWrite(work)) {
            Interlocked.Decrement(ref _pendingWorkCount);
            throw new ObjectDisposedException(nameof(WorldPersistence));
        }
    }

    private void WriteChunk(Chunk chunk, ChunkSaveKey key, TaskCompletionSource completion) {
        long start = Stopwatch.GetTimestamp();
        try {
            _provider.SaveChunk(chunk);
            completion.TrySetResult();
        }
        catch (Exception exception) {
            Logger.Err($"Failed to save chunk {chunk.X},{chunk.Z}: {exception.Message}");
            completion.TrySetException(exception);
        }
        finally {
            Interlocked.Add(ref _chunkWriteTicks, Stopwatch.GetTimestamp() - start);
            Interlocked.Increment(ref _chunkWriteCount);
            _chunkSaves.TryRemove(new KeyValuePair<ChunkSaveKey, TaskCompletionSource>(key, completion));
            _pendingChunks.TryRemove(new KeyValuePair<ChunkSaveKey, ChunkColumn>(key, chunk));
        }
    }

    private void WritePlayerData(string xuid, CompoundTag data) {
        try {
            _provider.SavePlayerData(xuid, data);
        }
        catch (Exception exception) {
            Logger.Err($"Failed to save player {xuid}: {exception.Message}");
        }
        finally {
            _playerSaves.TryRemove(new KeyValuePair<string, CompoundTag>(xuid, data));
        }
    }

    private void WriteSpawnPosition(SpawnWork spawn) {
        try {
            _provider.SaveSpawnPosition(spawn.Type, spawn.Position);
        }
        catch (Exception exception) {
            Logger.Err($"Failed to save {spawn.Type} spawn position: {exception.Message}");
        }
    }

    private void WaitForChunk(ChunkSaveKey key) {
        if (_chunkSaves.TryGetValue(key, out TaskCompletionSource? completion)) {
            completion.Task.GetAwaiter().GetResult();
        }
    }

    private abstract record PersistenceWork;
    private sealed record ChunkWork(Chunk Chunk, ChunkSaveKey Key, TaskCompletionSource Completion) : PersistenceWork;
    private sealed record PlayerWork(string Xuid, CompoundTag Data) : PersistenceWork;
    private sealed record SpawnWork(DimensionId Type, Vec3 Position) : PersistenceWork;
    private sealed record FlushWork(TaskCompletionSource Completion) : PersistenceWork;

    private readonly record struct ChunkSaveKey(DimensionId Type, int X, int Z);
}
