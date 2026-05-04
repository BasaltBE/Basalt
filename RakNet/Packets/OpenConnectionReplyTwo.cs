using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct OpenConnectionReplyTwo(long serverId = 0, Address clientAddress = default, ushort mtu = 0, bool serverSecurity = false)
{
    public const byte PacketId = 0x08;

    public long ServerId = serverId;
    public Address ClientAddress = clientAddress;
    public ushort MTU = mtu;
    public bool ServerSecurity = serverSecurity;

    public static OpenConnectionReplyTwo Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        offset += Magic.MAGIC_LENGTH;

        long ServerId = src.ReadInt64(offset, false);
        offset += 8;

        Address ClientAddress = Address.Read(src, out int AddressBytesRead, offset);
        offset += AddressBytesRead;

        ushort MTU = src.ReadUInt16(offset, false);
        offset += 2;

        bool ServerSecurity = src.ReadBool(offset);
        return new(ServerId, ClientAddress, MTU, ServerSecurity);
    }

    public static int Serialize(OpenConnectionReplyTwo packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        dest.WriteInt64(packet.ServerId, offset, false);
        offset += 8;

        offset += Address.Write(packet.ClientAddress, dest, offset);

        dest.WriteUInt16(packet.MTU, offset, false);
        offset += 2;

        dest.WriteBool(packet.ServerSecurity, offset);
        offset += 1;

        return offset;
    }
}
