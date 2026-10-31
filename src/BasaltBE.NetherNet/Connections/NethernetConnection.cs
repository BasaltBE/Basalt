using System.Buffers;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using BasaltBE.Logging;
using SIPSorcery.Net;

namespace BasaltBE.NetherNet.Connections;

public sealed class NethernetConnection : IAsyncDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("NetherNet");
    public const string ReliableChannelLabel = "ReliableDataChannel";
    public const string UnreliableChannelLabel = "UnreliableDataChannel";

    private readonly RTCPeerConnection peer;
    private readonly int maximumPacketLength;
    private readonly int reliableFragmentLength;
    private readonly int unreliableFragmentLength;
    private readonly int maximumQueuedSendBytes;
    private readonly Lock receiveSync = new();
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private byte[] fragments = [];
    private int fragmentLength;
    private RTCDataChannel? reliable;
    private RTCDataChannel? unreliable;
    private int expectedFragment = -1;
    private int channelCount;
    private int closed;
    private int disposed;
    private long queuedSendBytes;
    private readonly TaskCompletionSource<bool> channelsReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<Exception?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal NethernetConnection(
        RTCPeerConnection peer,
        int maximumPacketLength,
        int reliableFragmentLength,
        int unreliableFragmentLength,
        int maximumQueuedSendBytes = 16 * 1024 * 1024
    ) {
        this.peer = peer;
        Peer = default;
        this.maximumPacketLength = maximumPacketLength;
        this.reliableFragmentLength = reliableFragmentLength;
        this.unreliableFragmentLength = unreliableFragmentLength;
        this.maximumQueuedSendBytes = maximumQueuedSendBytes;
        peer.ondatachannel += Attach;
        peer.onconnectionstatechange += state => {
            if (state == RTCPeerConnectionState.failed) End(new IOException("WebRTC peer connection failed."));
            else if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.disconnected) End(null);
        };
    }

    internal Task ChannelsReady => channelsReady.Task;
    internal Task<Exception?> Completion => completion.Task;
    internal ulong OutgoingBufferedBytes => (ulong)Math.Max(0, Volatile.Read(ref queuedSendBytes)) +
        (reliable?.bufferedAmount ?? 0) + (unreliable?.bufferedAmount ?? 0);

    internal bool ReserveQueuedSendBytes(int length) {
        long current = Volatile.Read(ref queuedSendBytes);
        while (current + length <= maximumQueuedSendBytes) {
            long reserved = Interlocked.CompareExchange(ref queuedSendBytes, current + length, current);
            if (reserved == current)
                return true;
            current = reserved;
        }
        return false;
    }

    internal void ReleaseQueuedSendBytes(int length) => Interlocked.Add(ref queuedSendBytes, -length);

    internal Action<NethernetMessage>? MessageHandler { get; set; }
    internal void Close(Exception error) => End(error);

    public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, MessageReliability reliability, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref closed) != 0, this);
        int fragmentLength = reliability == MessageReliability.Reliable ? reliableFragmentLength : unreliableFragmentLength;
        RTCDataChannel channel = reliability == MessageReliability.Reliable ? reliable! : unreliable!;
        int fragmentCount = Math.Max(1, 1 + (payload.Length - 1) / fragmentLength);
        if (payload.Length > maximumPacketLength || fragmentCount > 256 ||
            (reliability == MessageReliability.Unreliable && fragmentCount != 1))
            throw new ArgumentOutOfRangeException(nameof(payload), "The message exceeds the NetherNet framing limit.");

        await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            int offset = 0;
            do {
                int length = Math.Min(fragmentLength, payload.Length - offset);
                int frameLength = length + 1;
                byte[] frame = ArrayPool<byte>.Shared.Rent(frameLength);
                try {
                    frame[0] = checked((byte)(fragmentCount - 1 - offset / fragmentLength));
                    payload.Span.Slice(offset, length).CopyTo(frame.AsSpan(1));
                    if (channel.readyState != RTCDataChannelState.open) throw new PeerDisconnectedException(Peer);
                    channel.send(frame, 0, frameLength);
                    Log.Debug($"Data-channel frame to {Peer.NetworkId}/{Peer.ConnectionId}: {channel.label}, {frameLength} bytes");
                }
                finally {
                    ArrayPool<byte>.Shared.Return(frame, clearArray: true);
                }
                offset += length;
            } while (offset < payload.Length);
        }
        finally {
            sendLock.Release();
        }
    }

    public ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return ValueTask.CompletedTask;
        End(new ObjectDisposedException(nameof(NethernetConnection)));
        peer.close();
        return ValueTask.CompletedTask;
    }

    internal void Attach(RTCDataChannel channel) {
        if (channel.label is not (ReliableChannelLabel or UnreliableChannelLabel)) {
            channel.close();
            End(new InvalidDataException($"Unknown NetherNet data channel: {channel.label}"));
            return;
        }
        bool isReliable = channel.label == ReliableChannelLabel;
        if (isReliable ? Interlocked.CompareExchange(ref reliable, channel, null) is not null : Interlocked.CompareExchange(ref unreliable, channel, null) is not null) {
            channel.close();
            End(new InvalidDataException("Duplicate NetherNet data channel."));
            return;
        }

        int opened = 0;
        void Opened() {
            if (Interlocked.Exchange(ref opened, 1) == 0 && Interlocked.Increment(ref channelCount) == 2)
                channelsReady.TrySetResult(true);
        }
        channel.onopen += Opened;
        if (channel.readyState == RTCDataChannelState.open) Opened();
        channel.onmessage += (dataChannel, protocol, data) => {
            Log.Debug($"Data-channel frame from {Peer.NetworkId}/{Peer.ConnectionId}: {dataChannel.label}, {protocol}, {data.Length} bytes");
            try {
                ReceiveFrame(data, isReliable);
            }
            catch (Exception error) {
                Log.Error($"Frame processing failed for {Peer.NetworkId}/{Peer.ConnectionId}: {error.Message}");
                End(error);
            }
        };
        channel.onclose += () => End(null);
    }

    private void ReceiveFrame(byte[] frame, bool isReliable) {
        if (Volatile.Read(ref closed) != 0) return;
        if (frame.Length == 0) { End(new InvalidDataException("NetherNet frame is empty.")); return; }
        lock (receiveSync) {
            int remaining = frame[0];
            int payloadLength = frame.Length - 1;
            int fragmentLimit = isReliable ? reliableFragmentLength : unreliableFragmentLength;
            if (payloadLength > fragmentLimit || payloadLength > maximumPacketLength) {
                End(new InvalidDataException("NetherNet frame exceeds the configured packet limit."));
                return;
            }
            if (!isReliable && remaining != 0) {
                fragmentLength = 0;
                expectedFragment = -1;
                End(new InvalidDataException("Unreliable messages cannot be fragmented."));
                return;
            }
            if (!isReliable) {
                MessageHandler?.Invoke(new NethernetMessage(Peer, MessageReliability.Unreliable, frame.AsMemory(1)));
                return;
            }
            if (expectedFragment >= 0 && remaining != expectedFragment - 1) {
                fragmentLength = 0;
                expectedFragment = -1;
                End(new InvalidDataException("NetherNet fragment order is invalid."));
                return;
            }
            if (fragmentLength + payloadLength > maximumPacketLength) {
                End(new InvalidDataException("NetherNet packet exceeds the configured limit."));
                return;
            }
            if (remaining == 0 && fragmentLength == 0) {
                expectedFragment = -1;
                MessageHandler?.Invoke(new NethernetMessage(Peer, MessageReliability.Reliable, frame.AsMemory(1)));
                return;
            }
            int required = fragmentLength + payloadLength;
            if (required > fragments.Length)
                Array.Resize(ref fragments, Math.Min(maximumPacketLength, Math.Max(required, fragments.Length == 0 ? 16 * 1024 : fragments.Length * 2)));
            frame.AsSpan(1).CopyTo(fragments.AsSpan(fragmentLength));
            fragmentLength += payloadLength;
            if (remaining != 0) {
                expectedFragment = remaining;
                return;
            }

            byte[] payload = GC.AllocateUninitializedArray<byte>(fragmentLength);
            fragments.AsSpan(0, fragmentLength).CopyTo(payload);
            fragmentLength = 0;
            expectedFragment = -1;
            MessageHandler?.Invoke(new NethernetMessage(Peer, MessageReliability.Reliable, payload));
        }
    }

    internal void SetPeer(NethernetPeer value) => Peer = value;

    public NethernetPeer Peer { get; private set; }

    private void End(Exception? error) {
        if (Interlocked.Exchange(ref closed, 1) != 0) return;
        channelsReady.TrySetException(error ?? new IOException("Connection closed before both data channels opened."));
        completion.TrySetResult(error);
    }
}

