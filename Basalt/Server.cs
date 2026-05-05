using Basalt.Network;
using Basalt.RakNet;

namespace Basalt.Core;

public sealed class Server
{
    private readonly NetworkServer _raknet;
    private readonly NetworkHandler _network;

    public Server()
    {
        _raknet = new NetworkServer();
        _network = new NetworkHandler(this);
        _raknet.OnMessage += _network.HandlePacket;
    }

    public void Start()
    {
        _raknet.Start().AsTask().Wait();
    }

  
}
