using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Worlds.Providers.Types;

public readonly record struct ChunkStorageKey(
    DimensionType DimensionType,
    string DimensionIdentifier,
    bool PrimaryStorageDimension,
    int X,
    int Z
);
