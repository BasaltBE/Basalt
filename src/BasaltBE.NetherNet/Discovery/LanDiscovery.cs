using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Threading.Channels;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Packets;
using BasaltBE.NetherNet.Types;
using SIPSorcery.Net;

namespace BasaltBE.NetherNet.Discovery;

internal sealed class LanDiscovery : IAsyncDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("Discovery");
    private readonly NethernetServerOptions options;
    private readonly IServerIdentity identity;
    private readonly Func<NethernetPeer, Connections.NethernetConnection, ValueTask> accepted;
    private readonly Socket socket;
    private readonly Channel<(DiscoveryPacket Packet, IPEndPoint Address)> packets;
    private readonly ConcurrentDictionary<ulong, IPEndPoint> addresses = new();
    private readonly ConcurrentDictionary<(ulong NetworkId, ulong ConnectionId), RTCPeerConnection> peers = new();
    private readonly ConcurrentDictionary<long, Task> offerTasks = new();
    private readonly SemaphoreSlim offerCapacity;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task receiveTask;
    private readonly Task processTask;
    private long nextOffer;

    public LanDiscovery(NethernetServerOptions options, IServerIdentity identity, ulong networkId, Func<NethernetPeer, Connections.NethernetConnection, ValueTask> accepted) {
        this.options = options;
        this.identity = identity;
        this.accepted = accepted;
        NetworkId = networkId;
        offerCapacity = new SemaphoreSlim(options.MaximumPeers, options.MaximumPeers);
        socket = new Socket(options.DiscoveryAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        socket.Bind(options.DiscoveryAddress);
        packets = Channel.CreateBounded<(DiscoveryPacket, IPEndPoint)>(new BoundedChannelOptions(256) {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = true
        });
        receiveTask = ReceiveAsync(cancellation.Token);
        processTask = ProcessAsync(cancellation.Token);
    }

    public ulong NetworkId { get; }

    public async ValueTask DisposeAsync() {
        cancellation.Cancel();
        socket.Dispose();
        packets.Writer.TryComplete();
        try {
            await Task.WhenAll(receiveTask, processTask).ConfigureAwait(false);
        } catch (Exception error) when (error is SocketException or ObjectDisposedException or OperationCanceledException) { }

        await Task.WhenAll(offerTasks.Values).ConfigureAwait(false);
        offerCapacity.Dispose();
        cancellation.Dispose();
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken) {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(DiscoveryPacketCodec.MaximumDatagramLength);
        EndPoint sender = options.DiscoveryAddress.AddressFamily == AddressFamily.InterNetworkV6
            ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0);
        while (!cancellationToken.IsCancellationRequested) {
            try {
                SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, sender, cancellationToken).ConfigureAwait(false);
                if (result.RemoteEndPoint is not IPEndPoint address ||
                    !DiscoveryPacketCodec.TryDecode(buffer.AsSpan(0, result.ReceivedBytes), out DiscoveryPacket? packet, options.ApplicationId) ||
                    packet is null)
                    continue;
                if (packet.SenderId != 0) addresses[packet.SenderId] = address;
                packets.Writer.TryWrite((packet, address));
            } catch (SocketException) when (!cancellationToken.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
            } catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { return; }
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken) {
        await foreach ((DiscoveryPacket packet, IPEndPoint address) in packets.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
            if (packet.SenderId == NetworkId) continue;
            if (packet.Type == DiscoveryPacketType.Request) {
                byte[] data = ServerAdvertisementCodec.Encode(options.Advertisement);
                await SendPacketAsync(new DiscoveryPacket(DiscoveryPacketType.Response, NetworkId, 0, data), address, cancellationToken).ConfigureAwait(false);
            } else if (packet.Type == DiscoveryPacketType.Message && packet.RecipientId == NetworkId &&
                       packet.Data.Length > 0 && !packet.Data.AsSpan().SequenceEqual("Ping"u8)) {
                if (NethernetSignal.TryDecode(packet.Data, packet.SenderId.ToString(CultureInfo.InvariantCulture), out NethernetSignal signal)) {
                    if (signal.Type == "CONNECTREQUEST") {
                        if (!offerCapacity.Wait(0)) {
                            await SendSignalAsync("CONNECTERROR", signal.ConnectionId, "1", packet.SenderId, address, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        long id = Interlocked.Increment(ref nextOffer);
                        Task task = AcceptOfferWithinCapacityAsync(signal, packet.SenderId, address, cancellationToken);
                        offerTasks[id] = task;
                        _ = task.ContinueWith(completedTask => offerTasks.TryRemove(id, out Task? removed), CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    } else if (signal.Type == "CANDIDATEADD" && peers.TryGetValue((packet.SenderId, signal.ConnectionId), out RTCPeerConnection? peer))
                        peer.addIceCandidate(new RTCIceCandidateInit { candidate = signal.Data });
                    else if (signal.Type == "CONNECTERROR" && peers.TryRemove((packet.SenderId, signal.ConnectionId), out RTCPeerConnection? failedPeer))
                        failedPeer.close();
                }
            }
        }
    }

    private async Task AcceptOfferWithinCapacityAsync(NethernetSignal signal, ulong remoteId, IPEndPoint address, CancellationToken cancellationToken) {
        try {
            await AcceptOfferAsync(signal, remoteId, address, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        } catch (Exception error) {
            Log.Error($"LAN offer from {remoteId} failed: {error.Message}");
        } finally {
            offerCapacity.Release();
        }
    }

    private async Task AcceptOfferAsync(NethernetSignal signal, ulong remoteId, IPEndPoint address, CancellationToken cancellationToken) {
        if (signal.Data.Length == 0 || signal.Data.Length > 8192 || !signal.Data.StartsWith("v=0", StringComparison.Ordinal)) {
            await SendSignalAsync("CONNECTERROR", signal.ConnectionId, "1", remoteId, address, cancellationToken).ConfigureAwait(false);
            return;
        }
        RTCPeerConnection transport = new(new RTCConfiguration { iceServers = [] });
        Connections.NethernetConnection connection = new(
            transport,
            options.MaximumPacketLength,
            options.ReliableFragmentLength,
            options.UnreliableFragmentLength,
            options.MaximumQueuedSendBytes
        );
        (ulong NetworkId, ulong ConnectionId) key = (remoteId, signal.ConnectionId);
        peers[key] = transport;
        transport.onicecandidate += candidate => {
            if (candidate is not null)
                _ = SendSignalAsync("CANDIDATEADD", signal.ConnectionId, candidate.candidate, remoteId, address, cancellationToken);
        };
        try {
            if (transport.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = signal.Data }) != SetDescriptionResultEnum.OK)
                throw new InvalidDataException("The LAN SDP offer was rejected.");
            await transport.setLocalDescription(transport.createAnswer(null)).ConfigureAwait(false);
            string answer = transport.localDescription?.sdp?.ToString() ?? throw new InvalidDataException("The LAN SDP answer is missing.");
            answer = StripCandidates(answer);
            answer = identity.AddAssertion(answer);
            connection.SetPeer(new NethernetPeer(remoteId, signal.ConnectionId, address.Address));
            await accepted(connection.Peer, connection).ConfigureAwait(false);
            await SendSignalAsync("CONNECTRESPONSE", signal.ConnectionId, answer, remoteId, address, cancellationToken).ConfigureAwait(false);
            _ = connection.Completion.ContinueWith(completed => { peers.TryRemove(key, out RTCPeerConnection? removed); }, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        } catch {
            peers.TryRemove(key, out _);
            await connection.DisposeAsync().ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested) {
                try {
                    await SendSignalAsync("CONNECTERROR", signal.ConnectionId, "1", remoteId, address, cancellationToken).ConfigureAwait(false);
                } catch (OperationCanceledException) { }
            }
        }
    }

    private async ValueTask SendSignalAsync(string type, ulong connectionId, string data, ulong recipient, IPEndPoint address, CancellationToken cancellationToken) {
        byte[] payload = Encoding.UTF8.GetBytes($"{type} {connectionId.ToString(CultureInfo.InvariantCulture)} {data}");
        await SendPacketAsync(new DiscoveryPacket(DiscoveryPacketType.Message, NetworkId, recipient, payload), address, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask SendPacketAsync(DiscoveryPacket packet, IPEndPoint address, CancellationToken cancellationToken) {
        byte[] datagram = DiscoveryPacketCodec.Encode(packet, options.ApplicationId);
        await socket.SendToAsync(datagram, SocketFlags.None, address, cancellationToken).ConfigureAwait(false);
    }

    private static string StripCandidates(string sdp) {
        StringBuilder result = new(sdp.Length);
        using StringReader lines = new(sdp);
        while (lines.ReadLine() is { } line)
            if (!line.StartsWith("a=candidate:", StringComparison.Ordinal) && line != "a=end-of-candidates") result.Append(line).Append("\r\n");
        return result.ToString();
    }

}
