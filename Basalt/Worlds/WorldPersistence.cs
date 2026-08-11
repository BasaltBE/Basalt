namespace Basalt.Core.Worlds;

using System.Collections.Concurrent;
using System.Threading.Channels;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.Protocol.Enums;
using BedrockProtocol.Nbt;
using BedrockProtocol.Types;

internal sealed class WorldPersistence : IDisposable {
    private readonly WorldProvider _provider;
    private readonly Channel<PersistenceWork> _queue;
    private readonly ConcurrentDictionary<ChunkSaveKey, TaskCompletionSource> _chunkSaves = new();
    private readonly ConcurrentDictionary<string, CompoundTag> _playerSaves = new(StringComparer.Ordinal);
    private readonly Task _worker;

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
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _chunkSaves[key] = completion;
        try {
            Write(new ChunkWork(chunk, key, completion));
        }
        catch {
            _chunkSaves.TryRemove(new KeyValuePair<ChunkSaveKey, TaskCompletionSource>(key, completion));
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
    }

    private void Write(PersistenceWork work) {
        if (!_queue.Writer.TryWrite(work)) {
            throw new ObjectDisposedException(nameof(WorldPersistence));
        }
    }

    private void WriteChunk(Chunk chunk, ChunkSaveKey key, TaskCompletionSource completion) {
        try {
            _provider.SaveChunk(chunk);
            completion.TrySetResult();
        }
        catch (Exception exception) {
            Logger.Err($"Failed to save chunk {chunk.X},{chunk.Z}: {exception.Message}");
            completion.TrySetException(exception);
        }
        finally {
            _chunkSaves.TryRemove(new KeyValuePair<ChunkSaveKey, TaskCompletionSource>(key, completion));
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
