using System.Net;
using System.Net.Sockets;
using Basalt.Binary;
using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;

namespace Basalt.RakNet
{
    internal class NetworkServerConnection : NetworkConnection
    {
        private const byte ConnectedPingPacketId = 0x00;
        private const byte ConnectedPongPacketId = 0x03;
        private const byte EncapsulatedGamePacketId = 0xFE;

        public long ClientId { get; }
        public SocketAddress Endpoint { get; }
        public ushort Mtu { get; }
        public bool IsConnected { get; private set; }
        public event Action<NetworkConnection>? Connected;
        public event Action<NetworkConnection>? Disconnected;
        public event Action<NetworkConnection, ReadOnlyMemory<byte>>? Message;

        private readonly Socket Socket;
        protected override int MaxMtu => Mtu;

        public NetworkServerConnection(Socket socket, SocketAddress endpoint, long clientId, ushort mtu)
        {
            Socket = socket;
            Endpoint = endpoint;
            ClientId = clientId;
            Mtu = mtu;
        }

        protected override void SendMessage(ReadOnlySpan<byte> raw)
        {
            Socket.SendTo(raw, SocketFlags.None, Endpoint);
        }

        protected override void HandleFrame(Packets.Types.Frame frame)
        {
            if (frame.Buffer.Length == 0)
            {
                return;
            }

            byte PacketId = frame.Buffer[0];

            if (PacketId == ConnectionRequest.PacketId)
            {
                ConnectionRequest request = ConnectionRequest.Deserialize(frame.Buffer);

                SocketAddress[] serverNetAddresses = new SocketAddress[20];
                for (int i = 0; i < serverNetAddresses.Length; i++)
                {
                    serverNetAddresses[i] = new SocketAddress(AddressFamily.InterNetwork);
                }

                ConnectionRequestAccepted accepted = new(
                    clientAddress: Endpoint,
                    clientIndex: 0,
                    serverNetAddresses: serverNetAddresses,
                    clientSendTime: request.ClientSendTime,
                    serverSendTime: (ulong)Environment.TickCount64
                );

                Span<byte> payload = stackalloc byte[2048];
                int length = ConnectionRequestAccepted.Serialize(accepted, payload);
                SendPayload(payload[..length]);
                return;
            }

            if (PacketId == NewIncomingConnection.PacketId)
            {
                _ = NewIncomingConnection.Deserialize(frame.Buffer);
                IsConnected = true;
                Connected?.Invoke(this);
                return;
            }

            if (PacketId == ConnectedPingPacketId && frame.Buffer.Length >= 9)
            {
                ulong clientPingTime = frame.Buffer.ReadUInt64(1, false);
                Span<byte> pong = stackalloc byte[17];
                pong.WriteUInt8(ConnectedPongPacketId, 0);
                pong.WriteUInt64(clientPingTime, 1, false);
                pong.WriteUInt64((ulong)Environment.TickCount64, 9, false);
                SendPayload(pong, Reliability.Unreliable);
                return;
            }

            if (PacketId == DisconnectNotification.PacketId)
            {
                IsConnected = false;
                Disconnected?.Invoke(this);
                return;
            }

            if (PacketId == EncapsulatedGamePacketId)
            {
                Message?.Invoke(this, frame.Buffer);
                return;
            }
        }
    }
}
