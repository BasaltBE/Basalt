using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Basalt.Binary;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetDiscoveryServer : IDisposable {
    private static readonly byte[] Magic = [
        0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe,
        0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78
    ];
    private readonly Func<ulong, string> _serverList;
    private readonly ulong _guid;
    private UdpClient[]? _listeners;
    private readonly CancellationTokenSource _cancellation = new();
    private Task[] _loops = [];

    public NetherNetDiscoveryServer(ushort ipv4Port, ushort ipv6Port, Func<ulong, string> serverList) {
        _serverList = serverList ?? throw new ArgumentNullException(nameof(serverList));
        Span<byte> guid = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(guid);
        _guid = BinaryPrimitives.ReadUInt64BigEndian(guid);
        _ipv4Port = ipv4Port;
        _ipv6Port = ipv6Port;
    }

    private readonly ushort _ipv4Port;
    private readonly ushort _ipv6Port;

    public void Start() {
        UdpClient ipv6 = new(AddressFamily.InterNetworkV6);
        ipv6.Client.DualMode = false;
        ipv6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, _ipv6Port));
        _listeners = [
            new UdpClient(new IPEndPoint(IPAddress.Any, _ipv4Port)),
            ipv6
        ];
        _loops = _listeners.Select(ListenAsync).ToArray();
    }

    public void Dispose() {
        _cancellation.Cancel();
        foreach (UdpClient listener in _listeners ?? []) {
            listener.Dispose();
        }

        _listeners = null;
    }

    private async Task ListenAsync(UdpClient listener) {
        while (!_cancellation.IsCancellationRequested) {
            UdpReceiveResult received;
            try {
                received = await listener.ReceiveAsync(_cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) {
                return;
            }
            catch (ObjectDisposedException) when (_cancellation.IsCancellationRequested) {
                return;
            }

            if (!TryCreatePong(received.Buffer, out byte[] pong)) {
                continue;
            }

            try {
                await listener.SendAsync(pong, received.RemoteEndPoint).ConfigureAwait(false);
            }
            catch (SocketException) when (_cancellation.IsCancellationRequested) {
                return;
            }
        }
    }

    private bool TryCreatePong(ReadOnlySpan<byte> ping, out byte[] pong) {
        pong = [];
        if (ping.Length < 33 || (ping[0] != 0x01 && ping[0] != 0x02) ||
            !ping.Slice(9, Magic.Length).SequenceEqual(Magic)) {
            return false;
        }

        byte[] serverName = Encoding.UTF8.GetBytes(_serverList(_guid));
        if (serverName.Length > ushort.MaxValue) {
            return false;
        }

        int length = 1 + 8 + 8 + Magic.Length + 2 + serverName.Length;
        pong = new byte[length];
        int offset = 0;
        BinaryWriter writer = new(pong, ref offset);
        writer.WriteUInt8(0x1c);
        writer.WriteBytes(ping.Slice(1, 8));
        writer.WriteUInt64(_guid, false);
        writer.WriteBytes(Magic);
        writer.WriteUInt16(checked((ushort)serverName.Length), false);
        writer.WriteBytes(serverName);
        return true;
    }
}
