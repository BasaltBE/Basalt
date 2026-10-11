using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt;

public sealed class Server : IDisposable {
    public readonly NetworkServer Network;

    public Server(NethernetServerOptions options, IServerIdentity? identity = null) {
        Network = new NetworkServer(options, identity);
    }

    public void Start() => Network.Start();

    public void Tick() {
        using ProfilerZone zone = Profiler.BeginZone("Server.Tick");
        Profiler.FrameMark();
    }

    public void Dispose() => Network.Dispose();
}
