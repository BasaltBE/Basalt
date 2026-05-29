using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Basalt.Binary;
using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet;

public enum ClientState
{
    Disconnected,
    ConnectingOne,
    ConnectingTwo,
    Handshaking,
    Connected
}

public class NetworkClient : NetworkConnection, IDisposable
{
    private const int DisconnectTimeoutMs = 15000;

    private Socket? _socket;
    private SocketAddress? _serverSocketAddress;
    private CancellationTokenSource? _cancellationTokenSource;
    private TaskCompletionSource<bool>? _connectTcs;
    private uint? _cookie;
    private bool _serverSecurity;
    private long _lastHandshakeSendMs;
    private int _handshakeAttempts;
    private long _lastConnectionRequestSendMs;
    private int _connectionRequestAttempts;
    private long _lastPingSendMs;
    private bool _closed = true;

    public ushort Mtu { get; private set; } = 1400;

    protected override int MaxMtu => Mtu;

    public bool IsConnected { get; private set; }
    public ClientState State { get; private set; } = ClientState.Disconnected;
    public ulong ClientGuid { get; } = (ulong)Random.Shared.NextInt64(1, long.MaxValue);
    public long LastSeenMs { get; private set; } = Environment.TickCount64;
    public SocketAddress? ServerEndpoint => _serverSocketAddress;

    public event Action<NetworkConnection>? Connected;
    public event Action<NetworkConnection>? Disconnected;
    public event Action<NetworkConnection, ReadOnlyMemory<byte>>? Message;

    public async Task Connect(string host, ushort port, CancellationToken cancellationToken = default)
    {
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];
        await Connect(new IPEndPoint(address, port), cancellationToken);
    }

    public async Task Connect(IPEndPoint remoteEP, CancellationToken cancellationToken = default)
    {
        if (State != ClientState.Disconnected)
            throw new InvalidOperationException("Client is already connecting or connected.");

        _serverSocketAddress = remoteEP.Serialize();
        _socket = new Socket(remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        if (OperatingSystem.IsWindows())
            _socket.IOControl(-1744830452, [0, 0, 0, 0], null);

        _socket.Bind(new IPEndPoint(
            remoteEP.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));

        State = ClientState.ConnectingOne;
        _closed = false;
        _handshakeAttempts = 0;
        _lastHandshakeSendMs = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        _connectTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        _ = Task.Run(() => TickLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        SendOpenConnectionRequestOne();

        using (cancellationToken.Register(() => _connectTcs.TrySetCanceled(cancellationToken)))
            await _connectTcs.Task;
    }

    protected override void SendMessage(ReadOnlySpan<byte> raw)
    {
        if (_socket is null || _serverSocketAddress is null) return;
        try
        {
            _socket.SendTo(raw, SocketFlags.None, _serverSocketAddress);
        }
        catch
        {
        }
    }

    protected override void HandleFrame(Frame frame)
    {
        if (frame.Buffer.Length == 0) return;

        LastSeenMs = Environment.TickCount64;
        byte packetId = frame.Buffer.Span[0];

        switch (packetId)
        {
            case ConnectionRequestAccepted.PacketId when State == ClientState.Handshaking:
                HandleConnectionRequestAccepted(frame.Buffer.Span);
                break;

            case ConnectedPing.PacketId:
                HandleConnectedPing(frame.Buffer.Span);
                break;

            case DisconnectNotification.PacketId:
                Disconnect(false);
                break;

            case 0xFE when IsConnected:
                Message?.Invoke(this, frame.Buffer);
                break;
        }
    }

    private void HandleConnectionRequestAccepted(ReadOnlySpan<byte> buffer)
    {
        try
        {
            var accepted = ConnectionRequestAccepted.Deserialize(buffer);

            /// Here the address count is 20
            var clientAddresses = new SocketAddress[20];
            for (int i = 0; i < clientAddresses.Length; i++)
                clientAddresses[i] = new SocketAddress(_socket!.AddressFamily);

            var incoming = new NewIncomingConnection(
                serverAddress: _serverSocketAddress!,
                clientNetAddresses: clientAddresses,
                clientSendTime: accepted.ClientSendTime,
                serverSendTime: accepted.ServerSendTime
            );

            Span<byte> payload = stackalloc byte[2048];
            int length = NewIncomingConnection.Serialize(incoming, payload);

            SendPayload(payload[..length], immediate: true);

            State = ClientState.Connected;
            IsConnected = true;

            _connectTcs?.TrySetResult(true);
            Connected?.Invoke(this);
        }
        catch (Exception ex)
        {
            _connectTcs?.TrySetException(ex);
            Disconnect(false);
        }
    }

    private void HandleConnectedPing(ReadOnlySpan<byte> buffer)
    {
        var ping = ConnectedPing.Deserialize(buffer);

        Span<byte> pong = stackalloc byte[17];
        ConnectedPong.Serialize(new ConnectedPong(ping.Time, Environment.TickCount64), pong);

        SendPayload(pong, Reliability.Unreliable);
        Tick(Environment.TickCount64);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[2048];
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var endpoint = new SocketAddress(_socket!.AddressFamily);
                int received = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, endpoint);
                if (received > 0)
                    HandleIncomingPacket(buffer.AsSpan(0, received), endpoint);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception) { }
        }
    }

    private void HandleIncomingPacket(ReadOnlySpan<byte> message, SocketAddress endpoint)
    {
        if (message.Length == 0) return;

        LastSeenMs = Environment.TickCount64;
        byte packetId = message[0];

        switch (State)
        {
            case ClientState.ConnectingOne when packetId == OpenConnectionReplyOne.PacketId:
                try
                {
                    var reply = OpenConnectionReplyOne.Deserialize(message);
                    _cookie = reply.Cookie;
                    Mtu = reply.MTU;
                    _handshakeAttempts = 0;
                    State = ClientState.ConnectingTwo;
                    SendOpenConnectionRequestTwo();
                }
                catch (Exception ex)
                {
                    _connectTcs?.TrySetException(ex);
                    Disconnect(false);
                }
                break;

            case ClientState.ConnectingTwo when packetId == OpenConnectionReplyTwo.PacketId:
                try
                {
                    OpenConnectionReplyTwo reply = OpenConnectionReplyTwo.Deserialize(message);
                    _serverSecurity = reply.ServerSecurity;
                    _handshakeAttempts = 0;
                    _connectionRequestAttempts = 0;
                    State = ClientState.Handshaking;
                    SendConnectionRequest();
                }
                catch (Exception ex)
                {
                    _connectTcs?.TrySetException(ex);
                    Disconnect(false);
                }
                break;

            case ClientState.Handshaking or ClientState.Connected:
                switch (packetId)
                {
                    case >= 0x80 and <= 0x8d:
                        try
                        {
                            HandleFrameSet(FrameSet.Deserialize(message));
                        }
                        catch (Exception) { }
                        break;
                    case Ack.PacketId:
                        HandleAck(Ack.Deserialize(message));
                        break;
                    case Nack.PacketId:
                        HandleNack(Nack.Deserialize(message));
                        break;
                    case DisconnectNotification.PacketId:
                        Disconnect(false);
                        break;
                }
                break;
        }
    }

    private void SendOpenConnectionRequestOne()
    {
        _lastHandshakeSendMs = Environment.TickCount64;
        _handshakeAttempts++;

        var packet = new OpenConnectionRequestOne(protocolVersion: 11, mtu: Mtu);
        byte[] buffer = new byte[Mtu - 28];
        OpenConnectionRequestOne.Serialize(packet, buffer);

        try { _socket?.SendTo(buffer, SocketFlags.None, _serverSocketAddress!); } catch { }
    }

    private void SendOpenConnectionRequestTwo()
    {
        _lastHandshakeSendMs = Environment.TickCount64;
        _handshakeAttempts++;

        var packet = new OpenConnectionRequestTwo(
            clientId: (long)ClientGuid,
            serverAddress: _serverSocketAddress!,
            cookie: _cookie,
            mtu: Mtu
        );

        byte[] buffer = new byte[Mtu - 28];
        int length = OpenConnectionRequestTwo.Serialize(packet, buffer);

        try { _socket?.SendTo(buffer.AsSpan(0, length), SocketFlags.None, _serverSocketAddress!); } catch { }
    }

    private void SendConnectionRequest()
    {
        _lastConnectionRequestSendMs = Environment.TickCount64;
        _connectionRequestAttempts++;

        var request = new ConnectionRequest(
            clientGuid: ClientGuid,
            clientSendTime: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            doSecurity: _serverSecurity
        );

        Span<byte> payload = stackalloc byte[2048];
        int length = ConnectionRequest.Serialize(request, payload);

        SendPayload(payload[..length], Reliability.ReliableOrdered, immediate: true);
    }

    private void SendConnectedPing()
    {
        Span<byte> ping = stackalloc byte[9];
        ConnectedPing.Serialize(new ConnectedPing(Environment.TickCount64), ping);

        SendPayload(ping, Reliability.Unreliable);
        Tick(Environment.TickCount64);
    }

    private async Task TickLoop(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            long now = Environment.TickCount64;

            try
            {
                if (State is ClientState.ConnectingOne or ClientState.ConnectingTwo)
                {
                    if (now - _lastHandshakeSendMs < 500) continue;

                    if (_handshakeAttempts >= 10)
                    {
                        string phase = State == ClientState.ConnectingOne ? "OpenConnectionReplyOne" : "OpenConnectionReplyTwo";
                        _connectTcs?.TrySetException(new TimeoutException($"Timed out waiting for {phase}."));
                        Disconnect(false);
                        return;
                    }

                    if (State == ClientState.ConnectingOne) SendOpenConnectionRequestOne();
                    else SendOpenConnectionRequestTwo();
                }
                else if (State is ClientState.Handshaking or ClientState.Connected)
                {
                    Tick(now);

                    if (now - LastSeenMs >= DisconnectTimeoutMs)
                    {
                        if (State == ClientState.Handshaking)
                            _connectTcs?.TrySetException(new TimeoutException("Timed out waiting for ConnectionRequestAccepted."));
                        Disconnect(false);
                        return;
                    }

                    if (State == ClientState.Connected && now - _lastPingSendMs >= 2000)
                    {
                        SendConnectedPing();
                        _lastPingSendMs = now;
                    }
                }
            }
            catch { }
        }
    }

    public override void Disconnect() => Disconnect(true);

    public void Disconnect(bool sendNotification = true)
    {
        if (_closed) return;

        _closed = true;
        State = ClientState.Disconnected;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;

        if (sendNotification && IsConnected)
        {
            try
            {
                Span<byte> buffer = stackalloc byte[1];
                int length = DisconnectNotification.Serialize(new DisconnectNotification(), buffer);
                SendPayload(buffer[..length], Reliability.Unreliable);
                Tick(Environment.TickCount64);
            }
            catch { }
        }

        IsConnected = false;

        if (_connectTcs is { Task.IsCompleted: false })
            _connectTcs.TrySetException(new Exception("Connection disconnected before completing handshake."));

        try { _socket?.Close(); } catch { }
        _socket = null;

        Disconnected?.Invoke(this);
    }

    public void Dispose()
    {
        Disconnect(true);
        GC.SuppressFinalize(this);
    }
}
