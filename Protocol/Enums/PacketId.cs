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
        Text = 0x09,
        StartGame = 0x0b,
        MovePlayer = 0x13,
        ActorEvent = 0x1b,
        UpdateAttributes = 0x1d,
        InventoryTransaction = 0x1e,
        Interact = 0x21,
        SetActorData = 0x27,
        LevelChunk = 0x3a,
        RequestChunkRadius = 0x45,
        ChunkRadiusUpdated = 0x46,
        SetLocalPlayerAsInitialized = 0x71,
        NetworkStackLatency = 0x73,
        AvailableActorIdentifiers = 0x77,
        NetworkChunkPublisherUpdate = 0x79,
        NetworkSettings = 0x8f,
        PlayerAuthInput = 0x90,
        ItemStackRequest = 0x93,
        ItemStackResponse = 0x94,
        CorrectPlayerMovePrediction = 0xa1,
        ItemRegistry = 0xa2,
        RequestNetworkSettings = 0xc1
    }
}
