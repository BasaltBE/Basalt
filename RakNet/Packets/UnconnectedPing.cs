using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct UnconnectedPing
{
    public const byte PacketId = 0x01;

    public long Time;
    public ulong Guid;

    public UnconnectedPing(long time, ulong guid)
    {
        Time = time;
        Guid = guid;
    }

    public static UnconnectedPing Deserialize(ReadOnlySpan<byte> src)
    {
        return new(src.ReadInt64(1, false), src.ReadUInt64(1 + 8 + Magic.MAGIC_LENGTH, false));
    }

    public static int Serialize(UnconnectedPing ping, Span<byte> dest)
    {
        dest.WriteUInt8(PacketId);
        dest.WriteInt64(ping.Time, 1, true);
        Magic.Write(dest, 1 + 8);
        dest.WriteUInt64(ping.Guid, 1 + 8 + Magic.MAGIC_LENGTH, true);

        // Return the size written
        return 1 + 8 + Magic.MAGIC_LENGTH + 8;
    }
}
