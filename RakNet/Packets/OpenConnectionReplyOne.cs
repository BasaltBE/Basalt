using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;


public struct OpenConnectionReplyOne(long serverId = 0, uint? cookie = null, ushort mtu = 0)
{
    public const byte PacketId = 0x06;

    public long ServerId = serverId;
    public uint? Cookie = cookie;
    public ushort MTU = mtu;

    public static OpenConnectionReplyOne Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        offset += Magic.MAGIC_LENGTH;

        long ServerId = src.ReadInt64(offset, false);
        offset += 8;

        bool HasSecurity = src.ReadBool(offset);
        offset += 1;

        uint? Cookie = null;
        if (HasSecurity)
        {
            Cookie = src.ReadUInt32(offset, false);
            offset += 4;
        }

        ushort MTU = src.ReadUInt16(offset, false);
        return new(ServerId, Cookie, MTU);
    }

    public static int Serialize(OpenConnectionReplyOne packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        dest.WriteInt64(packet.ServerId, offset, false);
        offset += 8;

        dest.WriteBool(packet.Cookie.HasValue, offset);
        offset += 1;

        if (packet.Cookie.HasValue)
        {
            dest.WriteUInt32(packet.Cookie.Value, offset, false);
            offset += 4;
        }

        dest.WriteUInt16(packet.MTU, offset, false);
        offset += 2;

        return offset;
    }
}
