namespace Basalt.Core.Network;

using Basalt.RakNet.Packets.Enums;

public delegate void NetworkSend(ReadOnlySpan<byte> payload, Reliability reliability, bool immediate);

public sealed class NetworkConnection {
    private readonly NetworkSend _send;
    private readonly Action _disconnect;

    internal NetworkConnection(NetworkSend send, Action disconnect, bool netherNet = false) {
        _send = send ?? throw new ArgumentNullException(nameof(send));
        _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
        NetherNet = netherNet;
    }

    internal bool NetherNet { get; }

    internal bool NetherNetCompression { get; set; }

    public void SendPacket(
        ReadOnlySpan<byte> payload,
        Reliability reliability,
        bool immediate = true) {
        _send(payload, reliability, immediate);
    }

    public void Disconnect() {
        _disconnect();
    }
}
