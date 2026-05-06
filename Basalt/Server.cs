using Basalt.Network;
using Basalt.Protocol.Enums;
using Basalt.RakNet;
using Basalt.World.Dimension.Provider;
using Basalt.World.Dimension.Generation;
using WorldInstance = Basalt.World.World;

namespace Basalt.Core;

public sealed class Server
{
    private readonly NetworkServer _raknet;
    private readonly NetworkHandler _network;

    public readonly Dictionary<NetworkConnection, Player> Players = new();

    public ServerOptions Options { get; }
    public NetworkHandler Network => _network;
    public WorldInstance World { get; }

    public Server(ServerOptions options = default)
    {
        Options = options == default ? new ServerOptions() : options;
        _raknet = new NetworkServer();
        _network = new NetworkHandler(this);
        _raknet.OnMessage += _network.HandlePacket;
        _raknet.OnDisconnected += _network.HandleDisconnected;

        World = CreateDefaultWorld();
    }

    public void Start()
    {
        _raknet.Start().AsTask().Wait();
    }

    public WorldInstance CreateWorld(string name, string providerIdentifier, params object[] providerArgs)
    {
        if (string.IsNullOrWhiteSpace(providerIdentifier))
        {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        Type providerType = providerIdentifier.ToLowerInvariant() switch
        {
            "leveldb" => typeof(LevelDbProvider),
            "memory" => typeof(InMemoryProvider),
            _ => throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.")
        };

        return CreateWorld(name, providerType, providerArgs);
    }

    public WorldInstance CreateWorld(string name, Type providerType, params object[] providerArgs)
    {
        if (!typeof(WorldProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException($"Provider type must inherit {nameof(WorldProvider)}.", nameof(providerType));
        }

        object? providerInstance = Activator.CreateInstance(providerType, providerArgs);
        if (providerInstance is not WorldProvider provider)
        {
            throw new InvalidOperationException($"Could not construct provider '{providerType.FullName}'.");
        }

        return new WorldInstance(name, provider);
    }

    private WorldInstance CreateDefaultWorld()
    {
        WorldInstance world = CreateWorld("world", "leveldb", "worlds/world");
        world.RegisterGenerator<VoidGenerator>("void");
        world.RegisterGenerator<SuperFlatGenerator>("superflat");
        world.CreateDimension("overworld", DimensionType.Overworld, "superflat");
        return world;
    }
}
