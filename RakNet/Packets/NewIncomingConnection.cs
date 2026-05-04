using Basalt.Binary;
using Basalt.RakNet.Packets.Types;
using System.Net;
using System.Net.Sockets;

namespace Basalt.RakNet.Packets;

public struct NewIncomingConnection(
    SocketAddress serverAddress,
    SocketAddress[]? clientNetAddresses = null,
    ulong clientSendTime = 0,
    ulong serverSendTime = 0
)
{
    public const byte PacketId = 0x13;

    public SocketAddress ServerAddress = serverAddress;
    public SocketAddress[] ClientNetAddresses = clientNetAddresses ?? new SocketAddress[20];
    public ulong ClientSendTime = clientSendTime;
    public ulong ServerSendTime = serverSendTime;

    public static NewIncomingConnection Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 0;
        byte packetId = src.ReadUInt8(offset);
        offset += 1;

        if (packetId != PacketId)
        {
            throw new InvalidOperationException("Invalid packet id.");
        }

        SocketAddress serverAddress = SocketAddress.Read(src, ref offset);

        SocketAddress[] clientNetAddresses = new SocketAddress[20];
        for (int i = 0; i < 20; i++)
        {
            clientNetAddresses[i] = SocketAddress.Read(src, ref offset);
        }

        ulong clientSendTime = src.ReadUInt64(offset, false);
        offset += 8;

        ulong serverSendTime = src.ReadUInt64(offset, false);

        return new(serverAddress, clientNetAddresses, clientSendTime, serverSendTime);
    }

    public static int Serialize(NewIncomingConnection packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        packet.ServerAddress.Write(dest, ref offset);

        for (int i = 0; i < 20; i++)
        {
            if (packet.ClientNetAddresses is not null && i < packet.ClientNetAddresses.Length)
            {
                packet.ClientNetAddresses[i].Write(dest, ref offset);
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
