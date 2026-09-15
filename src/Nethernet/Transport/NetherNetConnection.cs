namespace Basalt.Core.Nethernet;

public sealed class NetherNetConnection : IDisposable {
    private readonly NetherNetReassembler _reassembler = new();
    private readonly Action<byte[], int> _send;

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

        NetherNetFrame.Send(payload, MaximumPayloadSize, _send);
    }

    public void Receive(ReadOnlySpan<byte> frame) {
        if (Channel == NetherNetChannel.Unreliable) {
            if (NetherNetReassembler.AddUnreliable(frame, out ReadOnlySpan<byte> payload)) {
                MessageReceived?.Invoke(payload.ToArray());
            }
            return;
        }

        if (_reassembler.Add(frame, out byte[] reliablePayload)) {
            MessageReceived?.Invoke(reliablePayload);
        }
    }

    public void Close() {
        Closed?.Invoke();
    }

    public void Dispose() {
        _reassembler.Dispose();
    }
}
