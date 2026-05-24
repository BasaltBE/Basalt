using System;
using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct ConnectedPong(long pingTime = 0, long pongTime = 0)
{
    public const byte PacketId = 0x03;

    public long PingTime = pingTime;
    public long PongTime = pongTime;

    public static ConnectedPong Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 17 || src[0] != PacketId)
        {
            throw new InvalidOperationException("Invalid packet ID or length.");
        }
        return new(src.ReadInt64(1, false), src.ReadInt64(9, false));
    }

    public static int Serialize(ConnectedPong packet, Span<byte> dest)
    {
        dest.WriteUInt8(PacketId, 0);
        dest.WriteInt64(packet.PingTime, 1, false);
        dest.WriteInt64(packet.PongTime, 9, false);
        return 17;
    }
}
