using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct UnconnectedPong
{
    public const byte PacketId = 0x1c;

    public long Time;
    public ulong Guid;
    public string Advertisement;

    public static int Serialize(UnconnectedPong packet, Span<byte> dest)
    {
        dest.WriteUInt8(PacketId);
        dest.WriteInt64(packet.Time);
        dest.WriteUInt64(packet.Guid);
        return 0;
    }
}