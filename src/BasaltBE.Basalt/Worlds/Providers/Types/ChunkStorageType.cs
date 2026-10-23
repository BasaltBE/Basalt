namespace BasaltBE.Basalt.Worlds.Providers.Types;

public enum ChunkStorageType : byte {
    Data3D = 0x2B,
    Version = 0x2C,
    SubChunk = 0x2F,
    BlockActor = 0x31,
    FinalizedState = 0x36,
    MetadataHash = 0x3F,
    BlendingData = 0x40,
    ActorDigestVersion = 0x41
}
