namespace BasaltBE.Basalt.Worlds.Providers.Types;

public readonly record struct ChunkStorageEntry(ChunkStorageType Type, sbyte SubChunkY, byte[] Data);
