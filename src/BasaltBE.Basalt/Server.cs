using BasaltBE.Logging;
using BasaltBE.Basalt.Network;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt;

public sealed class Server : IDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("Server");
    public readonly NetworkServer Network;

    public Server(NethernetServerOptions options, IServerIdentity? identity = null) {
        Network = new NetworkServer(options, identity);
    }

    public void Start() => Network.Start(HandleMessage);

    public void Dispose() => Network.Dispose();

    public void HandleMessage(NethernetMessage message) {
        if (message.Connected)
            Log.Info($"Peer {message.Peer.ConnectionId} connected.");
        else if (message.Disconnected)
            Log.Info($"Peer {message.Peer.ConnectionId} disconnected.");
        else
            Log.Debug($"Received {message.Payload.Length} bytes from peer {message.Peer.ConnectionId}.");
    }
}
