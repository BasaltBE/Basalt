using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct UnconnectedPing(long time = 0, ulong guid = 0)
{
    public const byte PacketId = 0x01;

    public long Time = time;
    public ulong Guid = guid;

    public static UnconnectedPing Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        long Time = src.ReadInt64(offset, false);
        offset += 8;

        offset += Magic.MAGIC_LENGTH;

        ulong Guid = src.ReadUInt64(offset, false);
        return new(Time, Guid);
    }

    public static int Serialize(UnconnectedPing ping, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        dest.WriteInt64(ping.Time, offset, true);
        offset += 8;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        dest.WriteUInt64(ping.Guid, offset, true);
        offset += 8;

        return offset;
    }
}
