namespace BasaltBE.Basalt.Worlds.Providers.Types;

public readonly record struct ChunkStorageRecord(ChunkStorageKey Key, ChunkStorageEntry[] Entries);
