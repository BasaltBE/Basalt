namespace Basalt.Protocol.Enums
{
    public enum PacketId : byte
    {
        Login = 0x01,
        PlayStatus = 0x02,
        Disconnect = 0x05,
        ResourcePacksInfo = 0x06,
        ResourcePackStack = 0x07,
        ResourcePackClientResponse = 0x08,
        StartGame = 0x0b,
        LevelChunk = 0x3a,
        RequestChunkRadius = 0x45,
        ChunkRadiusUpdated = 0x46,
        SetLocalPlayerAsInitialized = 0x71,
        NetworkStackLatency = 0x73,
        NetworkChunkPublisherUpdate = 0x79,
        PlayerAuthInput = 0x90,
        NetworkSettings = 0x8f,
        RequestNetworkSettings = 0xc1
    }
}
