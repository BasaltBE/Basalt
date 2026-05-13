using System.Net;
using System.Net.Sockets;
using Basalt.Binary;
using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;

namespace Basalt.RakNet;

internal class NetworkServerConnection : NetworkConnection
{
    private const byte ConnectedPingPacketId = 0x00;
    private const byte ConnectedPongPacketId = 0x03;
    private const byte EncapsulatedGamePacketId = 0xFE;

    public long ClientId { get; }
    public SocketAddress Endpoint { get; }
    public ushort Mtu { get; }
    public bool IsConnected { get; private set; }
    public long LastSeenMs { get; private set; } = Environment.TickCount64;

    public event Action<NetworkConnection>? Connected;
    public event Action<NetworkConnection>? Disconnected;
    public event Action<NetworkConnection, ReadOnlyMemory<byte>>? Message;

    protected override int MaxMtu => Mtu;

    private readonly Socket _socket;
    private bool _closed;

    public NetworkServerConnection(Socket socket, SocketAddress endpoint, long clientId, ushort mtu)
    {
        _socket = socket;
        Endpoint = endpoint;
        ClientId = clientId;
        Mtu = mtu;
    }

    protected override void SendMessage(ReadOnlySpan<byte> raw)
    {
        try
        {
            _socket.SendTo(raw, SocketFlags.None, Endpoint);
        }
        catch
        {
        }
    }

    protected override void HandleFrame(Packets.Types.Frame frame)
    {
        if (frame.Buffer.Length == 0)
        {
            return;
        }

        LastSeenMs = Environment.TickCount64;
        byte packetId = frame.Buffer[0];

        try
        {
            switch (packetId)
            {
                case ConnectionRequest.PacketId:
                    HandleConnectionRequest(frame.Buffer);
                    break;

                case NewIncomingConnection.PacketId:
                    HandleNewIncomingConnection(frame.Buffer);
                    break;

                case ConnectedPingPacketId:
                    HandleConnectedPing(frame.Buffer);
                    break;

                case DisconnectNotification.PacketId:
                    Disconnect(false);
                    break;

                case EncapsulatedGamePacketId:
                    if (!IsConnected)
                    {
                        IsConnected = true;
                        Connected?.Invoke(this);
                    }

                    Message?.Invoke(this, frame.Buffer);
                    break;
            }
        }
        catch
        {
        }
    }

    private void HandleConnectionRequest(ReadOnlySpan<byte> buffer)
    {
        ConnectionRequest request = ConnectionRequest.Deserialize(buffer);

        SocketAddress[] serverAddresses = new SocketAddress[20];

        for (int i = 0; i < serverAddresses.Length; i++)
        {
            serverAddresses[i] = new SocketAddress(AddressFamily.InterNetwork);
        }

        ConnectionRequestAccepted accepted = new(
            clientAddress: Endpoint,
            clientIndex: 0,
            serverNetAddresses: serverAddresses,
            clientSendTime: request.ClientSendTime,
            serverSendTime: (ulong)Environment.TickCount64
        );

        Span<byte> payload = stackalloc byte[2048];
        int length = ConnectionRequestAccepted.Serialize(accepted, payload);

        SendPayload(payload[..length]);
    }

    private void HandleNewIncomingConnection(ReadOnlySpan<byte> buffer)
    {
        _ = NewIncomingConnection.Deserialize(buffer);

        if (IsConnected)
        {
            return;
        }

        IsConnected = true;
        Connected?.Invoke(this);
    }

    private void HandleConnectedPing(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 9)
        {
            return;
        }

        ulong clientPingTime = buffer.ReadUInt64(1, false);

        Span<byte> pong = stackalloc byte[17];

        pong.WriteUInt8(ConnectedPongPacketId, 0);
        pong.WriteUInt64(clientPingTime, 1, false);
        pong.WriteUInt64((ulong)Environment.TickCount64, 9, false);

        SendPayload(pong, Reliability.Unreliable);
    }

    public override void Disconnect()
    {
        Disconnect(true);
    }

    public void Disconnect(bool sendNotification = true)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;

        if (sendNotification)
        {
            Span<byte> buffer = stackalloc byte[1];
            int length = DisconnectNotification.Serialize(new DisconnectNotification(), buffer);
            SendPayload(buffer[..length], Reliability.Unreliable);
        }

        IsConnected = false;
        Disconnected?.Invoke(this);
    }
}
