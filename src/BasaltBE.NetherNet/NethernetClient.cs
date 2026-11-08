using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using BasaltBE.NetherNet.Connections;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using SIPSorcery.Net;

namespace BasaltBE.NetherNet;

public sealed class NethernetClient : IAsyncDisposable {
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly NethernetClientOptions options;
    private readonly Channel<NethernetMessage> incoming;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ulong networkId = RandomNetworkId();
    private NethernetConnection? connection;
    private Task? monitor;
    private int connected;
    private int disposed;

    public NethernetClient(NethernetClientOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.SignalingUri);
        if (!options.SignalingUri.IsAbsoluteUri ||
            options.SignalingUri.Scheme is not ("http" or "https") ||
            options.MaximumQueuedMessages < 1 || options.MaximumSdpLength < 1 ||
            options.MaximumPacketLength < 1 || options.ReliableFragmentLength is < 1 or > 262_143 ||
            options.UnreliableFragmentLength is < 1 or > 262_143 || options.NegotiationTimeout <= TimeSpan.Zero ||
            options.RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options));
        this.options = options;
        incoming = Channel.CreateBounded<NethernetMessage>(new BoundedChannelOptions(options.MaximumQueuedMessages) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public bool Connected => Volatile.Read(ref connected) == 1;

    public async Task ConnectAsync(CancellationToken cancellationToken = default, Func<string, string>? addIdentityAssertion = null) {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.CompareExchange(ref connected, -1, 0) != 0) throw new InvalidOperationException("NetherNet client has already connected.");

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        linked.CancelAfter(options.NegotiationTimeout);
        RTCPeerConnection? transport = null;
        NethernetConnection? dataConnection = null;
        try {
            if (options.ClientVersion is not null && options.ProtocolVersion is not null && options.Platform is not null) {
                UriBuilder discoveryUri = new(options.SignalingUri) {
                    Path = "v1/join",
                    Query = $"version={Uri.EscapeDataString(options.ClientVersion)}&protocol={options.ProtocolVersion.Value.ToString(CultureInfo.InvariantCulture)}&platform={options.Platform.Value.ToString(CultureInfo.InvariantCulture)}&id={networkId.ToString(CultureInfo.InvariantCulture)}",
                    Fragment = ""
                };
                using HttpResponseMessage discovery = await Http.GetAsync(discoveryUri.Uri, linked.Token).ConfigureAwait(false);
                if (!discovery.IsSuccessStatusCode)
                    throw new HttpRequestException($"NetherNet discovery failed with HTTP {(int)discovery.StatusCode}.");
            }

            UriBuilder signalingUri = new(options.SignalingUri) {
                Path = "v1/join",
                Query = "",
                Fragment = ""
            };
            using CancellationTokenSource requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
            requestTimeout.CancelAfter(options.RequestTimeout);
            using HttpResponseMessage capability = await Http.GetAsync(signalingUri.Uri, requestTimeout.Token).ConfigureAwait(false);
            if (!capability.IsSuccessStatusCode)
                throw new HttpRequestException($"NetherNet signaling capability check failed with HTTP {(int)capability.StatusCode}.");

            transport = new RTCPeerConnection(new RTCConfiguration { iceServers = [] });
            dataConnection = new NethernetConnection(
                transport,
                options.MaximumPacketLength,
                options.ReliableFragmentLength,
                options.UnreliableFragmentLength
            );
            connection = dataConnection;
            IPAddress? address = await ResolveAddressAsync(options.SignalingUri.Host, linked.Token).ConfigureAwait(false);
            dataConnection.SetPeer(new NethernetPeer(networkId, 0, address));
            dataConnection.MessageHandler = message => {
                if (!incoming.Writer.TryWrite(message))
                    dataConnection.Close(new IOException("NetherNet client receive queue is full."));
            };

            dataConnection.Attach(await transport.createDataChannel(NethernetConnection.ReliableChannelLabel, new RTCDataChannelInit { ordered = true }).ConfigureAwait(false));
            dataConnection.Attach(await transport.createDataChannel(NethernetConnection.UnreliableChannelLabel, new RTCDataChannelInit { ordered = false, maxRetransmits = 0 }).ConfigureAwait(false));
            RTCSessionDescriptionInit offer = transport.createOffer(null);
            await transport.setLocalDescription(offer).ConfigureAwait(false);
            await WaitForIceAsync(transport, linked.Token).ConfigureAwait(false);
            string localSdp = transport.localDescription?.sdp?.ToString() ?? throw new IOException("NetherNet SDP offer is missing.");
            if (addIdentityAssertion is not null)
                localSdp = addIdentityAssertion(localSdp);
            if (Encoding.UTF8.GetByteCount(localSdp) > options.MaximumSdpLength)
                throw new InvalidDataException("NetherNet SDP offer exceeds the configured size limit.");

            signalingUri = new UriBuilder(options.SignalingUri) {
                Path = $"v1/join/{networkId.ToString(CultureInfo.InvariantCulture)}",
                Query = "",
                Fragment = ""
            };
            using HttpRequestMessage request = new(HttpMethod.Post, signalingUri.Uri) {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes(localSdp))
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sdp"));
            using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestTimeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"NetherNet signaling failed with HTTP {(int)response.StatusCode}.");
            byte[] answerBytes = await ReadBoundedAsync(response.Content, options.MaximumSdpLength, requestTimeout.Token).ConfigureAwait(false);
            if (answerBytes.Length == 0 || answerBytes.Length > options.MaximumSdpLength)
                throw new InvalidDataException("NetherNet SDP answer length is invalid.");
            string answerSdp = Encoding.UTF8.GetString(answerBytes);
            if (int.TryParse(answerSdp.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int failureReason)) {
                throw new InvalidDataException($"NetherNet signaling was rejected with response code {failureReason}.");
            }
            int identityStart = answerSdp.IndexOf("a=identity:", StringComparison.Ordinal);
            if (identityStart >= 0) {
                int identityEnd = answerSdp.IndexOfAny(['\r', '\n'], identityStart);
                answerSdp = identityEnd < 0
                    ? answerSdp[..identityStart]
                    : answerSdp.Remove(identityStart, identityEnd - identityStart);
            }
            if (transport.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = answerSdp }) != SetDescriptionResultEnum.OK)
                throw new InvalidDataException("NetherNet SDP answer was rejected.");

            await dataConnection.ChannelsReady.WaitAsync(linked.Token).ConfigureAwait(false);
            if (Volatile.Read(ref disposed) != 0) throw new ObjectDisposedException(nameof(NethernetClient));
            Volatile.Write(ref connected, 1);
            monitor = MonitorConnectionAsync(dataConnection);
        } catch {
            Volatile.Write(ref connected, 0);
            if (dataConnection is not null)
                await dataConnection.DisposeAsync().ConfigureAwait(false);
            else
                transport?.close();
            connection = null;
            throw;
        }
    }

    public ValueTask<NethernetMessage> ReceiveAsync(CancellationToken cancellationToken = default) => incoming.Reader.ReadAsync(cancellationToken);

    public ValueTask SendAsync(
        ReadOnlyMemory<byte> payload,
        MessageReliability reliability = MessageReliability.Reliable,
        CancellationToken cancellationToken = default
    ) {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (!Connected || connection is null) throw new InvalidOperationException("NetherNet client is not connected.");
        return connection.SendAsync(payload, reliability, cancellationToken);
    }

    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Volatile.Write(ref connected, 0);
        lifetime.Cancel();
        if (connection is not null) await connection.DisposeAsync().ConfigureAwait(false);
        if (monitor is not null) {
            try { await monitor.ConfigureAwait(false); } catch (Exception error) when (error is IOException or ObjectDisposedException) { }
        }
        incoming.Writer.TryComplete();
        lifetime.Dispose();
    }

    private async Task MonitorConnectionAsync(NethernetConnection dataConnection) {
        Exception? error = await dataConnection.Completion.ConfigureAwait(false);
        Volatile.Write(ref connected, 0);
        incoming.Writer.TryWrite(new NethernetMessage(dataConnection.Peer, MessageReliability.Unreliable, ReadOnlyMemory<byte>.Empty, Disconnected: true));
        if (error is not null and not ObjectDisposedException)
            incoming.Writer.TryComplete(error);
        else
            incoming.Writer.TryComplete();
    }

    private static async Task WaitForIceAsync(RTCPeerConnection peer, CancellationToken cancellationToken) {
        if (peer.iceGatheringState == RTCIceGatheringState.complete) return;
        TaskCompletionSource<bool> gathered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void StateChanged(RTCIceGatheringState state) {
            if (state == RTCIceGatheringState.complete) gathered.TrySetResult(true);
        }
        peer.onicegatheringstatechange += StateChanged;
        try {
            if (peer.iceGatheringState == RTCIceGatheringState.complete) return;
            await gathered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        } finally { peer.onicegatheringstatechange -= StateChanged; }
    }

    private static async ValueTask<IPAddress?> ResolveAddressAsync(string host, CancellationToken cancellationToken) {
        if (IPAddress.TryParse(host, out IPAddress? address)) return address;
        try { return (await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false)).FirstOrDefault(); } catch (SocketException) { return null; }
    }

    private static ulong RandomNetworkId() {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximumLength, CancellationToken cancellationToken) {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream output = new(Math.Min(maximumLength, 16 * 1024));
        byte[] buffer = GC.AllocateUninitializedArray<byte>(16 * 1024);
        while (true) {
            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) return output.ToArray();
            if (output.Length + read > maximumLength) throw new InvalidDataException("NetherNet SDP answer exceeds the configured size limit.");
            output.Write(buffer, 0, read);
        }
    }
}
