namespace Basalt.Core.Network;

public delegate void NetworkSend(ReadOnlySpan<byte> payload, bool unreliable, bool immediate);

public sealed class NetworkConnection {
    private readonly NetworkSend _send;
    private readonly NetworkSend? _unreliableSend;
    private readonly Action _disconnect;

    internal NetworkConnection(
        NetworkSend send,
        Action disconnect,
        NetworkSend? unreliableSend = null) {
        _send = send ?? throw new ArgumentNullException(nameof(send));
        _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
        _unreliableSend = unreliableSend;
    }

    internal bool NetherNetCompression { get; set; }

    public void SendPacket(
        ReadOnlySpan<byte> payload,
        bool unreliable,
        bool immediate = true) {
        (_unreliableSend is not null && unreliable
            ? _unreliableSend
            : _send)(payload, unreliable, immediate);
    }

    public void Disconnect() {
        _disconnect();
    }
}
