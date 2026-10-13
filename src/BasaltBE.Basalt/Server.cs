using System.Net;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;

namespace BasaltBE.Basalt;

public sealed class Server : IDisposable {
    public readonly ServerProperties Properties;
    public readonly NetworkServer Network;
    public readonly ChunkGeneratorRegistry Generators = new();
    public readonly List<World> Worlds = [];

    public Server(ServerProperties properties, IServerIdentity? identity = null) {
        Properties = properties;
        ArgumentException.ThrowIfNullOrWhiteSpace(Properties.DefaultWorldName);
        CreateWorld(Properties.DefaultWorldName);

        Network = new NetworkServer(this, new NethernetServerOptions {
            Advertisement = new ServerAdvertisement {
                Name = Properties.ServerName,
                Protocol = BedrockProtocol.ProtocolVersion,
                Version = BedrockProtocol.MinecraftVersion,
                Level = Properties.LevelName,
                MaxPlayers = Properties.MaxPlayers
            },
            HttpAddress = new IPEndPoint(IPAddress.Any, Properties.ServerPort),
            DiscoveryAddress = new IPEndPoint(IPAddress.Any, Properties.DiscoveryPort),
            IdentityKeyPath = Properties.IdentityKeyPath
        }, identity, ResourcePackManager.Load(Properties.ResourcePacksPath));
    }

    public World CreateWorld(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (Worlds.Any(world => string.Equals(world.Identifier, identifier, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"World '{identifier}' already exists.");

        World world = new(identifier, Generators);
        Worlds.Add(world);
        return world;
    }

    public World GetWorld() => GetWorld(Properties.DefaultWorldName);

    public World GetWorld(string identifier) {
        foreach (World world in Worlds) {
            if (string.Equals(world.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                return world;
        }

        throw new KeyNotFoundException($"World '{identifier}' was not found.");
    }

    public Player? GetPlayer(string username) {
        foreach (World world in Worlds) {
            Player? player = world.GetPlayer(username);
            if (player is not null)
                return player;
        }

        return null;
    }

    public Player? GetPlayerByXuid(string xuid) {
        foreach (World world in Worlds) {
            Player? player = world.GetPlayerByXuid(xuid);
            if (player is not null)
                return player;
        }

        return null;
    }

    public void Start() {
        World world = GetWorld();
        _ = world.GetDimension("minecraft:overworld") ?? world.CreateDimension("minecraft:overworld");
        Network.Start();
    }

    public void Tick() {
        using ProfilerZone zone = Profiler.BeginZone("Server.Tick");
        foreach (World world in Worlds)
            world.Tick();
        Network.Tick();
        Profiler.FrameMark();
    }

    public void Dispose() {
        Network.Dispose();
        foreach (World world in Worlds)
            world.Dispose();
    }
}
