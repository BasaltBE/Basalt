using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct UnconnectedPing(long time=0, ulong guid=0)
{
    public const byte PacketId = 0x01;

    public long Time = time;
    public ulong Guid = guid;

    public static UnconnectedPing Deserialize(ReadOnlySpan<byte> src)
    {
        return new(src.ReadInt64(1, false), src.ReadUInt64(1 + 8 + Magic.MAGIC_LENGTH, false));
    }

    public static int Serialize(UnconnectedPing ping, Span<byte> dest)
    {
        // For now we do write packet ids as well
        dest.WriteUInt8(PacketId);
        dest.WriteInt64(ping.Time, 1, true);
        Magic.Write(dest, 1 + 8);
        dest.WriteUInt64(ping.Guid, 1 + 8 + Magic.MAGIC_LENGTH, true);

        // Return the size written, we don't need to specify offset as its
        // just [offset..] and the same code just moved by tghe offset anyway
        return 1 + 8 + Magic.MAGIC_LENGTH + 8;
    }
}
