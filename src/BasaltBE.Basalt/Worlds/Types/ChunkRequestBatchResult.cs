namespace BasaltBE.Basalt.Worlds.Types;

public readonly struct ChunkRequestBatchResult(int queued, int alreadyLoaded, int alreadyPending, int queueFull) {
    public readonly int Queued = queued;
    public readonly int AlreadyLoaded = alreadyLoaded;
    public readonly int AlreadyPending = alreadyPending;
    public readonly int QueueFull = queueFull;
}
