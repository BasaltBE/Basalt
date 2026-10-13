using System.Collections.Concurrent;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Types;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed class Dimension : IDisposable {
    readonly Dictionary<long, Chunk?> chunks = [];
    readonly ConcurrentQueue<ChunkGenerationResult> completed = new();
    readonly ChunkGenerationService generationService;
    readonly bool ownsGenerationService;
    int chunkCount;
    int pendingRequestCount;

    public readonly string Identifier;
    public readonly IChunkGenerator Generator;

    public Dimension(string identifier, IChunkGenerator generator) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        Identifier = identifier;
        Generator = generator;
        generationService = new ChunkGenerationService(4, 1024);
        ownsGenerationService = true;
    }

    internal Dimension(string identifier, IChunkGenerator generator, ChunkGenerationService generationService)
        : this(identifier, generator, generationService, false) { }

    Dimension(string identifier, IChunkGenerator generator, ChunkGenerationService generationService, bool ownsGenerationService) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(generationService);
        Identifier = identifier;
        Generator = generator;
        this.generationService = generationService;
        this.ownsGenerationService = ownsGenerationService;
    }

    public int ChunkCount => chunkCount;
    public int PendingRequestCount => pendingRequestCount;

    public bool Empty {
        get {
            foreach (Chunk? chunk in chunks.Values) {
                if (chunk is not null && !chunk.Empty) {
                    return false;
                }
            }

            return true;
        }
    }

    public Chunk? GetChunk(int chunkX, int chunkZ) =>
        chunks.GetValueOrDefault(GetKey(chunkX, chunkZ));

    public Chunk GetOrCreateChunk(int chunkX, int chunkZ) {
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null) {
            return chunk;
        }

        chunk = new Chunk(chunkX, chunkZ);
        chunks[key] = chunk;
        chunkCount++;
        return chunk;
    }

    public ChunkRequestStatus Request(int chunkX, int chunkZ) {
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk)) {
            return chunk is null ? ChunkRequestStatus.AlreadyPending : ChunkRequestStatus.AlreadyLoaded;
        }

        if (generationService.Request(this, Generator, key, chunkX, chunkZ)) {
            chunks.Add(key, null);
            pendingRequestCount++;
            return ChunkRequestStatus.Queued;
        }

        return ChunkRequestStatus.QueueFull;
    }

    public ChunkRequestStatus Request(ChunkPos position) => Request(position.X, position.Z);

    public ChunkRequestBatchResult Request(ReadOnlySpan<ChunkPos> positions) {
        int queued = 0;
        int alreadyLoaded = 0;
        int alreadyPending = 0;
        int queueFull = 0;
        foreach (ChunkPos position in positions) {
            long key = GetKey(position.X, position.Z);
            if (chunks.TryGetValue(key, out Chunk? existing)) {
                if (existing is null) {
                    alreadyPending++;
                } else {
                    alreadyLoaded++;
                }
                continue;
            }

            if (generationService.Request(this, Generator, key, position.X, position.Z)) {
                chunks.Add(key, null);
                pendingRequestCount++;
                queued++;
            } else {
                queueFull++;
            }
        }

        return new ChunkRequestBatchResult(queued, alreadyLoaded, alreadyPending, queueFull);
    }

    public int DrainGeneratedChunks() {
        int committed = 0;
        Exception? error = null;
        while (completed.TryDequeue(out ChunkGenerationResult result)) {
            pendingRequestCount--;
            generationService.ReleaseRequest();
            if (result.Error is not null) {
                if (chunks.TryGetValue(result.Key, out Chunk? pendingChunk) && pendingChunk is null) {
                    chunks.Remove(result.Key);
                }
                error ??= result.Error;
                continue;
            }

            if (chunks.TryGetValue(result.Key, out Chunk? current) && current is null) {
                if (result.Chunk is not null) {
                    chunks[result.Key] = result.Chunk;
                    chunkCount++;
                    committed++;
                } else {
                    chunks.Remove(result.Key);
                }
            }
        }

        if (error is not null) {
            throw new InvalidOperationException("Chunk generation failed.", error);
        }

        return committed;
    }

    internal void CompleteGeneration(long key, Chunk? chunk, Exception? error) =>
        completed.Enqueue(new ChunkGenerationResult(key, chunk, error));

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        return chunks.TryGetValue(GetKey(chunkX, chunkZ), out Chunk? chunk) && chunk is not null
            ? chunk.GetPermutation(x & 15, y, z & 15, layer)
            : BlockStorage.Air;
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0) {
        ArgumentNullException.ThrowIfNull(permutation);
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        if (!chunks.TryGetValue(GetKey(chunkX, chunkZ), out Chunk? chunk) || chunk is null) {
            if (permutation == BlockStorage.Air) {
                return;
            }

            chunk = GetOrCreateChunk(chunkX, chunkZ);
        }

        chunk.SetPermutation(x & 15, y, z & 15, permutation, layer);
    }

    static long GetKey(int x, int z) => ((long)x << 32) | (uint)z;

    public void Dispose() {
        if (ownsGenerationService) {
            generationService.Dispose();
        }
    }
}
