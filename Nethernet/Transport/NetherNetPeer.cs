using SIPSorcery.Net;
using System.Collections.Concurrent;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetPeer : IDisposable {
    public const string ReliableDataChannel = "ReliableDataChannel";
    public const string UnreliableDataChannel = "UnreliableDataChannel";
    public const int ReliablePayloadSize = 262143;
    public const int UnreliablePayloadSize = 262143;

    private readonly RTCPeerConnection _peerConnection;
    private readonly ServerIdentity? _identity;
    private readonly ClientIdentity? _clientIdentity;
    private readonly ConcurrentDictionary<NetherNetChannel, NetherNetConnection> _connections = new();
    private readonly TaskCompletionSource _channelsOpened = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private int _openedChannels;
    private bool _disposed;

    public event Action<NetherNetConnection>? ChannelOpened;

    public event Action? Closed;

    public NetherNetPeer(ServerIdentity? identity = null, ClientIdentity? clientIdentity = null) {
        _identity = identity;
        _clientIdentity = clientIdentity;
        RTCConfiguration configuration = new() {
            iceServers = [],
            iceTransportPolicy = RTCIceTransportPolicy.all,
            bundlePolicy = RTCBundlePolicy.max_bundle,
            iceCandidatePoolSize = 0
        };
        _peerConnection = new RTCPeerConnection(configuration);
        _peerConnection.ondatachannel += HandleDataChannel;
        _peerConnection.oniceconnectionstatechange += state =>
            Logger.Info($"NetherNet ICE state: {state}.");
        _peerConnection.onicegatheringstatechange += state =>
            Logger.Debug($"NetherNet ICE gathering state: {state}.");
        _peerConnection.onconnectionstatechange += HandleConnectionState;
    }

    public async Task<string> CreateOfferAsync(CancellationToken cancellationToken = default) {
        EnsureNotDisposed();

        await CreateChannels().ConfigureAwait(false);
        RTCSessionDescriptionInit offerDescription = _peerConnection.createOffer(
            new RTCOfferOptions { X_WaitForIceGatheringToComplete = true });
        await _peerConnection.setLocalDescription(offerDescription).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        string offer = _peerConnection.localDescription.sdp.ToString();
        return _clientIdentity?.AddIdentity(offer) ?? offer;
    }

    public async Task<string> AcceptOfferAsync(
        string offer,
        CancellationToken cancellationToken = default) {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(offer);

        SetDescriptionResultEnum result = _peerConnection.setRemoteDescription(
            new RTCSessionDescriptionInit {
                sdp = offer,
                type = RTCSdpType.offer
            });
        if (result != SetDescriptionResultEnum.OK) {
            throw new InvalidOperationException($"NetherNet offer was rejected: {result}.");
        }

        RTCSessionDescriptionInit answerDescription = _peerConnection.createAnswer(
            new RTCAnswerOptions { X_WaitForIceGatheringToComplete = true });
        await _peerConnection.setLocalDescription(answerDescription).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        string answer = _peerConnection.localDescription.sdp.ToString();
        return _identity?.AddIdentity(answer) ?? answer;
    }

    public void AcceptAnswer(string answer) {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);

        SetDescriptionResultEnum result = _peerConnection.setRemoteDescription(
            new RTCSessionDescriptionInit {
                sdp = answer,
                type = RTCSdpType.answer
            });
        if (result != SetDescriptionResultEnum.OK) {
            throw new InvalidOperationException($"NetherNet answer was rejected: {result}.");
        }
    }

    public NetherNetConnection GetChannel(NetherNetChannel channel) {
        EnsureNotDisposed();
        return _connections.TryGetValue(channel, out NetherNetConnection? connection)
            ? connection
            : throw new InvalidOperationException($"The {channel} data channel is not open.");
    }

    public async Task WaitForChannelsAsync(CancellationToken cancellationToken = default) {
        EnsureNotDisposed();
        await _channelsOpened.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _peerConnection.ondatachannel -= HandleDataChannel;
        _peerConnection.onconnectionstatechange -= HandleConnectionState;
        _channelsOpened.TrySetException(new ObjectDisposedException(nameof(NetherNetPeer)));
        foreach (NetherNetConnection connection in _connections.Values) {
            connection.Dispose();
        }

        _connections.Clear();
        _peerConnection.Close("NetherNet peer disposed");
        _peerConnection.Dispose();
    }

    private async Task CreateChannels() {
        AttachDataChannel(await _peerConnection.createDataChannel(
            ReliableDataChannel,
            new RTCDataChannelInit {
                ordered = true,
                negotiated = false
            }).ConfigureAwait(false));
        AttachDataChannel(await _peerConnection.createDataChannel(
            UnreliableDataChannel,
            new RTCDataChannelInit {
                ordered = false,
                maxRetransmits = 0,
                negotiated = false
            }).ConfigureAwait(false));
    }

    private void HandleDataChannel(RTCDataChannel channel) {
        AttachDataChannel(channel);
    }

    private void AttachDataChannel(RTCDataChannel channel) {
        Logger.Info($"NetherNet data channel received: {channel.label}.");
        NetherNetChannel netherNetChannel = channel.label switch {
            ReliableDataChannel => NetherNetChannel.Reliable,
            UnreliableDataChannel => NetherNetChannel.Unreliable,
            _ => throw new InvalidOperationException($"Unsupported NetherNet channel '{channel.label}'.")
        };

        NetherNetConnection connection = new(
            netherNetChannel,
            netherNetChannel == NetherNetChannel.Reliable
                ? ReliablePayloadSize
            : UnreliablePayloadSize,
            (payload, length) => channel.send(payload, 0, length));
        if (!_connections.TryAdd(netherNetChannel, connection)) {
            connection.Dispose();
            return;
        }

        channel.onmessage += (_, _, payload) => connection.Receive(payload);
        channel.onopen += () => {
            Logger.Info($"NetherNet {netherNetChannel} data channel opened.");
            if (Interlocked.Increment(ref _openedChannels) == 2) {
                _channelsOpened.TrySetResult();
            }
        };
        channel.onclose += connection.Close;
        ChannelOpened?.Invoke(connection);
    }

    private void HandleConnectionState(RTCPeerConnectionState state) {
        Logger.Info($"NetherNet peer connection state: {state}.");
        if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.failed) {
            _channelsOpened.TrySetException(
                new InvalidOperationException($"NetherNet peer connection failed: {state}."));
            Closed?.Invoke();
        }
    }

    private void EnsureNotDisposed() {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
