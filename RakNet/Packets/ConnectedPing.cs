using System;
using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct ConnectedPing(long time = 0)
{
    public const byte PacketId = 0x00;

    public long Time = time;

    public static ConnectedPing Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 9 || src[0] != PacketId)
        {
            throw new InvalidOperationException("Invalid packet ID or length.");
        }
        return new(src.ReadInt64(1, false));
    }

    public static int Serialize(ConnectedPing packet, Span<byte> dest)
    {
        dest.WriteUInt8(PacketId, 0);
        dest.WriteInt64(packet.Time, 1, false);
        return 9;
    }
}
