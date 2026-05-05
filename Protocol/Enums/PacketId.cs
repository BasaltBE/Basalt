namespace Basalt.Protocol.Enums
{
    public enum PacketId : byte
    {
        Login = 0x01,
        PlayStatus = 0x02,
        Disconnect = 0x05,
        ResourcePacksInfo = 0x06,
        ResourcePackStack = 0x07,
        NetworkSettings = 0x8f,
        RequestNetworkSettings = 0xc1
    }
}
