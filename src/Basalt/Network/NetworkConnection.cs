namespace Basalt.Core.Network;

public delegate void NetworkSend(ReadOnlySpan<byte> payload, bool unreliable, bool immediate);
internal delegate void NetworkSendOwned(byte[] payload, int length, bool unreliable, bool immediate);

public sealed class NetworkConnection {
    private readonly NetworkSend _send;
    private readonly NetworkSendOwned? _sendOwned;
    private readonly NetworkSend? _unreliableSend;
    private readonly Action _disconnect;

    internal NetworkConnection(
        NetworkSend send,
        Action disconnect,
        NetworkSend? unreliableSend = null,
        NetworkSendOwned? sendOwned = null) {
        _send = send ?? throw new ArgumentNullException(nameof(send));
        _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
        _unreliableSend = unreliableSend;
        _sendOwned = sendOwned;
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

    internal bool SendOwned(byte[] payload, int length, bool unreliable, bool immediate) {
        if (_sendOwned is null) {
            return false;
        }

        _sendOwned(payload, length, unreliable, immediate);
        return true;
    }
}
