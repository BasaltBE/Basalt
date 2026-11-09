namespace BasaltBE.Basalt.Worlds.Providers.Types;

internal enum WorldStorageOperation : byte {
    Read,
    Write,
    WriteGroup,
    WriteEntities,
    Delete,
    WriteWorldData,
    Flush
}
