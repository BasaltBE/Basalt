namespace Basalt.RakNet.Packets;

public struct UnconnectedPong
{
    public const byte PacketId = 0x1c;

    public long Time;
    public ulong Guid;
    public string Advertisement;
}