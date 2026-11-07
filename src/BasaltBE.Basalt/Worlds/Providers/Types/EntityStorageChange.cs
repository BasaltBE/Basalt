namespace BasaltBE.Basalt.Worlds.Providers.Types;

public readonly record struct EntityStorageChange(long Id, ChunkStorageKey? Previous, ChunkStorageKey? Current, byte[]? Data);
