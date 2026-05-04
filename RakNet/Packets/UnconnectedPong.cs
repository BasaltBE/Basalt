using Basalt.Binary;
using System.Text;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct UnconnectedPong(long time = 0, ulong guid = 0, string advertisement = "")
{
    public const byte PacketId = 0x1c;

    public long Time = time;
    public ulong Guid = guid;
    public string Advertisement = advertisement;

    public static int Serialize(UnconnectedPong packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        dest.WriteInt64(packet.Time, offset, true);
        offset += 8;

        dest.WriteUInt64(packet.Guid, offset, true);
        offset += 8;

        int AdvertisementByteLength = Encoding.UTF8.GetByteCount(packet.Advertisement);
        dest.WriteUInt16((ushort)AdvertisementByteLength, offset, false);
        offset += 2;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        dest.WriteString(packet.Advertisement, AdvertisementByteLength, offset);
        offset += AdvertisementByteLength;

        return offset;
    }

    public static UnconnectedPong Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        long Time = src.ReadInt64(offset, true);
        offset += 8;

        ulong Guid = src.ReadUInt64(offset, true);
        offset += 8;

        ushort AdvertisementLength = src.ReadUInt16(offset, false);
        offset += 2;

        offset += Magic.MAGIC_LENGTH;
        
        return new(
            Time,
            Guid,
            src.ReadString(AdvertisementLength, offset)
        );
    }
}
