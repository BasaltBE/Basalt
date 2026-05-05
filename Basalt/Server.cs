using Basalt.Network;
using Basalt.RakNet;

namespace Basalt.Core;

public sealed class Server
{
    private readonly NetworkServer _raknet;
    private readonly NetworkHandler _network;

    public readonly Dictionary<NetworkConnection, Player> Players = new();

    public ServerOptions Options { get; }
    public NetworkHandler Network => _network;

    public Server(ServerOptions options = default)
    {
        Options = options == default ? new ServerOptions() : options;
        _raknet = new NetworkServer();
        _network = new NetworkHandler(this);
        _raknet.OnMessage += _network.HandlePacket;
        _raknet.OnDisconnected += _network.HandleDisconnected;
    }

    public void Start()
    {
        _raknet.Start().AsTask().Wait();
    }
}
