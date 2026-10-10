using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Network;

public sealed class NetworkServer : IDisposable {
    private readonly NethernetServer nethernet;

    public NetworkServer(NethernetServerOptions options, IServerIdentity? identity = null) {
        nethernet = new NethernetServer(options, identity);
    }

    public ulong NetworkId => nethernet.NetworkId;

    public void Start(Action<NethernetMessage> handler) => nethernet.Start(handler);

    public void SendPacket(
        NethernetPeer peer,
        ReadOnlyMemory<byte> packet,
        MessageReliability reliability = MessageReliability.Reliable
    ) => nethernet.Send(peer, packet, reliability);

    public void Dispose() => nethernet.Dispose();
}
