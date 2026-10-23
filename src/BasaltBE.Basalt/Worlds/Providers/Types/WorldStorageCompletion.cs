using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Worlds.Providers.Types;

internal readonly record struct WorldStorageCompletion(
    WorldStorageOperation Operation,
    ChunkStorageKey Key,
    long RequestId,
    long Version,
    Chunk? Chunk,
    int DataBytes,
    bool Deleted,
    Exception? Error
);
