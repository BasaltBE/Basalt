namespace Basalt.Core.Nethernet;

public sealed class NetherNetConnection : IDisposable {
    private readonly NetherNetReassembler _reassembler = new();
    private readonly Action<byte[], int> _send;
    private int _receivedFramesLogged;
    private int _sentMessagesLogged;

    public NetherNetChannel Channel { get; }

    public int MaximumPayloadSize { get; }

    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    public event Action? Closed;

    public NetherNetConnection(
        NetherNetChannel channel,
        int maximumPayloadSize,
        Action<byte[], int> send) {
        Channel = channel;
        MaximumPayloadSize = maximumPayloadSize > NetherNetFrame.HeaderSize
            ? maximumPayloadSize
            : throw new ArgumentOutOfRangeException(nameof(maximumPayloadSize));
        _send = send ?? throw new ArgumentNullException(nameof(send));
    }

    public void Send(ReadOnlySpan<byte> payload) {
        if (Channel == NetherNetChannel.Unreliable && payload.Length > MaximumPayloadSize) {
            throw new ArgumentOutOfRangeException(nameof(payload), "Unreliable NetherNet messages cannot be fragmented.");
        }

        if (Interlocked.Exchange(ref _sentMessagesLogged, 1) == 0) {
            Logger.Info($"NetherNet {Channel} sent its first message ({payload.Length} bytes).");
        }

        NetherNetFrame.Send(payload, MaximumPayloadSize, _send);
    }

    public void Receive(ReadOnlySpan<byte> frame) {
        if (Interlocked.Exchange(ref _receivedFramesLogged, 1) == 0) {
            Logger.Info($"NetherNet {Channel} received its first frame ({frame.Length} bytes).");
        }

        if (_reassembler.Add(frame, out byte[] payload)) {
            MessageReceived?.Invoke(payload);
        }
    }

    public void Close() {
        Closed?.Invoke();
    }

    public void Dispose() {
        _reassembler.Dispose();
    }
}
