using System.Collections.Concurrent;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

internal sealed class ChunkGenerationService : IDisposable {
    readonly BlockingCollection<GenerationRequest> requests;
    readonly Thread[] workers;
    readonly long maximumOutstanding;
    long outstanding;
    int disposed;

    internal ChunkGenerationService(int workerCount, int queueCapacity) {
        if (workerCount < 1) {
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        }

        if (queueCapacity < 1) {
            throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        }

        requests = new BlockingCollection<GenerationRequest>(new ConcurrentQueue<GenerationRequest>(), queueCapacity);
        workers = new Thread[workerCount];
        maximumOutstanding = (long)queueCapacity + workerCount;
        for (int i = 0; i < workers.Length; i++) {
            workers[i] = new Thread(Work) {
                IsBackground = true,
                Name = $"Basalt chunk generation {i}"
            };
            workers[i].Start();
        }
    }

    internal bool Request(Dimension dimension, IChunkGenerator generator, long key, int chunkX, int chunkZ) {
        if (Volatile.Read(ref disposed) != 0) {
            throw new ObjectDisposedException(nameof(ChunkGenerationService));
        }

        if (Interlocked.Increment(ref outstanding) > maximumOutstanding) {
            Interlocked.Decrement(ref outstanding);
            return false;
        }

        if (requests.TryAdd(new GenerationRequest(dimension, generator, key, chunkX, chunkZ))) {
            return true;
        }

        Interlocked.Decrement(ref outstanding);
        return false;
    }

    internal void ReleaseRequest() => Interlocked.Decrement(ref outstanding);

    public void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) {
            return;
        }

        requests.CompleteAdding();
        while (requests.TryTake(out _))
            Interlocked.Decrement(ref outstanding);

        foreach (Thread worker in workers)
            worker.Join();
        requests.Dispose();
    }

    void Work() {
        foreach (GenerationRequest request in requests.GetConsumingEnumerable()) {
            try {
                Chunk chunk = request.Generator.Generate(request.ChunkX, request.ChunkZ);
                if (chunk.X != request.ChunkX || chunk.Z != request.ChunkZ) {
                    throw new InvalidOperationException("A chunk generator returned a chunk with different coordinates.");
                }

                request.Dimension.CompleteGeneration(request.Key, chunk, null);
            }
            catch (Exception exception) {
                request.Dimension.CompleteGeneration(request.Key, null, exception);
            }
        }
    }

    readonly record struct GenerationRequest(
        Dimension Dimension,
        IChunkGenerator Generator,
        long Key,
        int ChunkX,
        int ChunkZ
    );
}

internal readonly record struct ChunkGenerationResult(long Key, Chunk? Chunk, Exception? Error);
