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
    Exception? Error,
    long GroupId = 0,
    ChunkStorageKey[]? GroupKeys = null,
    long[]? GroupVersions = null,
    EntityStorageRecord[]? Entities = null,
    EntityStorageChange[]? EntityChanges = null
);
