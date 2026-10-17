using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Events;

public static class PacketEvents
{
    public static readonly PacketEvent<CommandRequestPacket> CommandRequest = new(77);
    public static readonly PacketEvent<TextPacket> Text = new(9);
    public static readonly PacketEvent<ClientCacheStatusPacket> ClientCacheStatus = new();
    public static readonly PacketEvent<AnimatePacket> Animate = new(44);
    public static readonly PacketEvent<ContainerClosePacket> ContainerClose = new();
    public static readonly PacketEvent<InteractPacket> Interact = new();
    public static readonly PacketEvent<InventoryTransactionPacket> InventoryTransaction = new(30);
    public static readonly PacketEvent<MobEquipmentPacket> MobEquipment = new(31);
    public static readonly PacketEvent<ItemStackRequestPacket> ItemStackRequest = new();
    public static readonly PacketEvent<LoginPacket> Login = new();
    public static readonly PacketEvent<PlayerAuthInputPacket> PlayerAuthInput = new();
    public static readonly PacketEvent<PlayStatusPacket> PlayStatus = new();
    public static readonly PacketEvent<RequestChunkRadiusPacket> RequestChunkRadius = new();
    public static readonly PacketEvent<SetLocalPlayerAsInitializedPacket> SetLocalPlayerAsInitialized = new();
    public static readonly PacketEvent<RequestNetworkSettingsPacket> RequestNetworkSettings = new();
    public static readonly PacketEvent<ResourcePackChunkDataPacket> ResourcePackChunkData = new();
    public static readonly PacketEvent<ResourcePackChunkRequestPacket> ResourcePackChunkRequest = new();
    public static readonly PacketEvent<ResourcePackClientResponsePacket> ResourcePackClientResponse = new();
    public static readonly PacketEvent<ResourcePackDataInfoPacket> ResourcePackDataInfo = new();
    public static readonly PacketEvent<ResourcePacksInfoPacket> ResourcePacksInfo = new();
    public static readonly PacketEvent<ResourcePackStackPacket> ResourcePackStack = new();
    public static readonly PacketEvent<NetworkSettingsPacket> NetworkSettings = new();
}
