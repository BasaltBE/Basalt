namespace BasaltBE.Basalt.Worlds.Providers.Types;

internal enum WorldStorageOperation : byte {
    Read,
    Write,
    WriteGroup,
    Delete,
    WriteWorldData,
    Flush
}
