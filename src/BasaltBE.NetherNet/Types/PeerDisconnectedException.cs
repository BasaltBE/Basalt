namespace BasaltBE.NetherNet.Types;

public sealed class PeerDisconnectedException(NethernetPeer peer) : InvalidOperationException("Peer is not connected.") {
    public readonly NethernetPeer Peer = peer;
}
