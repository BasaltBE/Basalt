using System.Reflection;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Tests;

internal sealed class MultiplayerFixture : IDisposable
{
    internal readonly Server Server;
    internal readonly World World;
    internal readonly Dimension Dimension;
    private ulong nextPlayer;

    internal MultiplayerFixture(ushort port = 0)
    {
        Server = new Server(new ServerProperties { ServerPort = port, DiscoveryPort = 0 }, new Identity());
        Server.Generators.Register<VoidGenerator>("void");
        Server.Generators.SetDefault("void");
        World = Server.CreateWorld("test", world => world.CreateDimension("minecraft:overworld"));
        Dimension = World.GetDimension("minecraft:overworld")!;
    }

    internal Player CreatePlayer(NethernetPeer? peer = null, byte[]? skin = null)
    {
        ulong id = ++nextPlayer;
        Player player = new(Server.Network, World, Dimension, peer ?? new NethernetPeer(id, id),
            "Player" + id, id.ToString(), Guid.NewGuid(), 8, "test", skin ?? [])
        {
            Spawned = true,
            Listed = true
        };
        return player;
    }

    internal static HashSet<long> LoadedChunks(PlayerChunkRenderingTrait trait) =>
        (HashSet<long>)typeof(PlayerChunkRenderingTrait).GetField("loadedChunks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(trait)!;

    public void Dispose() => Server.Dispose();

    private sealed class Identity : IServerIdentity
    {
        public string AddAssertion(string sdp) => sdp;
        public void Dispose() { }
    }
}
