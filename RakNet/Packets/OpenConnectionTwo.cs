using Basalt.Binary;
using Basalt.RakNet.Packets.Types;
using System.Net;

namespace Basalt.RakNet.Packets;

public struct OpenConnectionRequestTwo(long clientId, SocketAddress serverAddress, uint? cookie = null, ushort mtu = 0)
{
    public const byte PacketId = 0x07;

    public long ClientId = clientId;
    public SocketAddress ServerAddress = serverAddress;
    public uint? Cookie = cookie;
    public ushort MTU = mtu;

    public static OpenConnectionRequestTwo Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        offset += Magic.MAGIC_LENGTH;

        int Remaining = src.Length - offset;
        uint? Cookie = null;
        if (Remaining != 17 && Remaining != 39)
        {
            Cookie = src.ReadUInt32(offset, false);
            offset += 4;

            offset += 1;
        }

        SocketAddress ServerAddress = SocketAddress.Read(src, ref offset);

        ushort MTU = src.ReadUInt16(offset, false);
        offset += 2;

        long ClientId = src.ReadInt64(offset, false);
        return new(ClientId, ServerAddress, Cookie, MTU);
    }

    public static int Serialize(OpenConnectionRequestTwo packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        if (packet.Cookie.HasValue)
        {
            dest.WriteUInt32(packet.Cookie.Value, offset, false);
            offset += 4;

            dest.WriteUInt8(0, offset);
            offset += 1;
        }

        packet.ServerAddress.Write(dest, ref offset);

        dest.WriteUInt16(packet.MTU, offset, false);
        offset += 2;

        dest.WriteInt64(packet.ClientId, offset, false);
        offset += 8;

        return offset;
    }
}
