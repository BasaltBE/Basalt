using BasaltBE.Basalt.Worlds.Providers.Types;

namespace BasaltBE.Basalt.Worlds.Providers;

public interface IWorldProvider : IDisposable {
    /// <summary>
    /// The name of the provider
    /// .</summary>
    string Identifier { get; }

    /// <summary>
    /// Returns the native Bedrock records for a chunk, or null when it is not stored.
    /// </summary>
    ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key);

    /// <summary>
    /// Writes a batch of owned chunk records.
    /// </summary>
    void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks);

    /// <summary>
    /// Deletes one stored chunk record.
    /// </summary>
    bool DeleteChunk(ChunkStorageKey key);

    /// <summary>
    /// Flushes buffered writes being disposed.
    /// </summary>
    void Flush();
}
