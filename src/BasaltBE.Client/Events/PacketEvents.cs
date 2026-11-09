using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Events;

public static class PacketEvents {
    public static readonly PacketEvent<ChunkRadiusUpdatedPacket> ChunkRadiusUpdated = new();
    public static readonly PacketEvent<DisconnectPacket> Disconnect = new();
    public static readonly PacketEvent<NetworkSettingsPacket> NetworkSettings = new();
    public static readonly PacketEvent<PlayStatusPacket> PlayStatus = new();
    public static readonly PacketEvent<ResourcePacksInfoPacket> ResourcePacksInfo = new();
    public static readonly PacketEvent<ResourcePackStackPacket> ResourcePackStack = new();
    public static readonly PacketEvent<ServerToClientHandshakePacket> ServerToClientHandshake = new();
    public static readonly PacketEvent<StartGamePacket> StartGame = new();
}
