using Basalt.Binary;
using Basalt.RakNet.Packets.Types;
using System.Net;
using System.Net.Sockets;

namespace Basalt.RakNet.Packets;

public struct ConnectionRequestAccepted(
    SocketAddress clientAddress,
    ushort clientIndex = 0,
    SocketAddress[]? serverNetAddresses = null,
    ulong clientSendTime = 0,
    ulong serverSendTime = 0
)
{
    public const byte PacketId = 0x10;

    public SocketAddress ClientAddress = clientAddress;
    public ushort ClientIndex = clientIndex;
    public SocketAddress[] ServerNetAddresses = serverNetAddresses ?? new SocketAddress[20];
    public ulong ClientSendTime = clientSendTime;
    public ulong ServerSendTime = serverSendTime;

    public static ConnectionRequestAccepted Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 0;
        byte packetId = src.ReadUInt8(offset);
        offset += 1;

        if (packetId != PacketId)
        {
            throw new InvalidOperationException("Invalid packet id.");
        }

        SocketAddress clientAddress = SocketAddress.Read(src, ref offset);
        ushort clientIndex = src.ReadUInt16(offset, false);
        offset += 2;

        SocketAddress[] serverNetAddresses = new SocketAddress[20];
        for (int i = 0; i < 20; i++)
        {
            serverNetAddresses[i] = SocketAddress.Read(src, ref offset);
        }

        ulong clientSendTime = src.ReadUInt64(offset, false);
        offset += 8;

        ulong serverSendTime = src.ReadUInt64(offset, false);

        return new(clientAddress, clientIndex, serverNetAddresses, clientSendTime, serverSendTime);
    }

    public static int Serialize(ConnectionRequestAccepted packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        packet.ClientAddress.Write(dest, ref offset);

        dest.WriteUInt16(packet.ClientIndex, offset, false);
        offset += 2;

        for (int i = 0; i < 20; i++)
        {
            if (packet.ServerNetAddresses is not null && i < packet.ServerNetAddresses.Length)
            {
                packet.ServerNetAddresses[i].Write(dest, ref offset);
            }
            else
            {
                new SocketAddress(AddressFamily.InterNetwork).Write(dest, ref offset);
            }
        }

        dest.WriteUInt64(packet.ClientSendTime, offset, false);
        offset += 8;

        dest.WriteUInt64(packet.ServerSendTime, offset, false);
        offset += 8;

        return offset;
    }
}
