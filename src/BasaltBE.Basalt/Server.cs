using System.Net;
using System.Diagnostics;
using BasaltBE.Basalt.Commands;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Basalt.Permissions;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt;

public sealed class Server : IDisposable {
    private int stopping;
    public readonly ServerProperties Properties;
    public readonly NetworkServer Network;
    public readonly EventEmitter Events = new();
    public readonly ChunkGeneratorRegistry Generators = new();
    public readonly List<World> Worlds = [];
    public readonly CommandRegistry Commands;
    public readonly PermissionStore Permissions;
    internal ulong TickValue;
    public readonly TickMetrics Ticks = new();
    public bool Stopping => Volatile.Read(ref stopping) != 0;

    public Server(ServerProperties properties, IServerIdentity? identity = null, PermissionStore? permissions = null) {
        Commands = new CommandRegistry(this);
        Permissions = permissions ?? new PermissionStore();
        Properties = properties;
        ArgumentException.ThrowIfNullOrWhiteSpace(Properties.DefaultWorldName);

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

    public World CreateWorld(string identifier, Action<World> configure) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(configure);
        foreach (World existing in Worlds) {
            if (string.Equals(existing.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"World '{identifier}' already exists.");
        }

        World world = new(identifier, Generators);
        Worlds.Add(world);
        try {
            world.Initialize(configure, Events);
        }
        catch {
            Worlds.Remove(world);
            world.Dispose();
            throw;
        }
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

    public void Broadcast(DataPacket packet, Player? exclude = null) {
        ArgumentNullException.ThrowIfNull(packet);
        foreach (World world in Worlds) {
            foreach (Dimension dimension in world.Dimensions.Values)
                dimension.Broadcast(packet, exclude);
        }
    }

    public void Start() {
        Commands.Prepare();
        Network.Start();
    }

    public void Stop() {
        Interlocked.Exchange(ref stopping, 1);
    }

    public void Tick() {
        long start = Stopwatch.GetTimestamp();
        using ProfilerZone zone = Profiler.BeginZone("Server.Tick");
        TickValue++;
        Commands.Tick();
        foreach (World world in Worlds)
            world.Tick();
        Network.Tick();
        Profiler.FrameMark();
        Ticks.Record(start);
    }

    public void Dispose() {
        Network.Dispose();
        foreach (World world in Worlds)
            world.Dispose();
        Events.Clear();
    }
}
