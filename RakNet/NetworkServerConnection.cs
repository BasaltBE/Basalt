using System.Net;
using System.Net.Sockets;
using Basalt.Binary;
using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;

namespace Basalt.RakNet;

internal sealed class NetworkServerConnection : NetworkConnection
{
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
        _socket.SendTo(raw, SocketFlags.None, Endpoint);
    }

    protected override void HandleFrame(Packets.Types.Frame frame)
    {
        if (frame.Buffer.Length == 0)
        {
            return;
        }

        LastSeenMs = Environment.TickCount64;
        byte packetId = frame.Buffer.Span[0];


        switch (packetId)
        {
            case ConnectionRequest.PacketId:
                HandleConnectionRequest(frame.Buffer.Span);
                break;

            case NewIncomingConnection.PacketId:
                HandleNewIncomingConnection(frame.Buffer.Span);
                break;

            case ConnectedPing.PacketId:
                HandleConnectedPing(frame.Buffer.Span);
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
            clientSendTime: unchecked((ulong)request.ClientSendTime),
            serverSendTime: (ulong)Environment.TickCount64
        );

        Span<byte> payload = stackalloc byte[2048];
        int length = ConnectionRequestAccepted.Serialize(accepted, payload);

        SendPayload(payload[..length], immediate: true);
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
        ConnectedPing ping = ConnectedPing.Deserialize(buffer);

        Span<byte> pong = stackalloc byte[17];
        ConnectedPong.Serialize(new ConnectedPong(ping.Time, Environment.TickCount64), pong);

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
