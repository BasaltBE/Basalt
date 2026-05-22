using Basalt.Network;
using Basalt.Protocol.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.World.Dimension;
using Basalt.World.Dimension.Provider;
using Basalt.World.Dimension.Generation;
using Basalt.World;
using System.Diagnostics;
using WorldInstance = Basalt.World.World;

namespace Basalt.Core;

public sealed class Server
{
    /// <summary>
    /// Raknet server
    /// </summary>
    private readonly NetworkServer _raknet;
    /// <summary>
    /// Registry for dimension generators
    /// </summary>
    private readonly Dictionary<string, Type> _generatorRegistry = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Registry for world providers
    /// </summary>
    private readonly Dictionary<string, Type> _providerRegistry = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Cancellation source for the main network loop
    /// </summary>
    private CancellationTokenSource? _runCancellation;
    /// <summary>
    /// Task for the main network loop
    /// </summary>
    private Task? _networkLoopTask;
    /// <summary>
    /// Cancellation source for the tick loop
    /// </summary>
    private CancellationTokenSource? _tickCancellation;
    /// <summary>
    /// Task for the tick loop
    /// </summary>
    private Task? _tickLoopTask;
    /// <summary>
    /// Registry for players
    /// </summary>
    public readonly Dictionary<NetworkConnection, Player> Players = new();
    /// <summary>
    /// Network handler for processing minecraft packets and packet handlers
    /// </summary>
    public NetworkHandler Network { get; }
    /// <summary>
    /// Server options
    /// </summary>
    public ServerOptions Options { get; }
    /// <summary>
    /// World
    /// </summary>
    public WorldInstance World { get; }

    public Server(ServerOptions options = default)
    {
        Options = options == default ? new ServerOptions() : options;
        _raknet = new NetworkServer();
        Network = new NetworkHandler(this);

        RegisterProvider<LevelDbProvider>("leveldb");
        RegisterProvider<InMemoryProvider>("memory");
        RegisterGenerator<VoidGenerator>("void");
        RegisterGenerator<SuperFlatGenerator>("superflat");


        World = CreateWorld("world", "leveldb", "worlds/world");

        if (!_generatorRegistry.TryGetValue("superflat", out Type? generatorType))
        {
            throw new KeyNotFoundException("No generator registered with identifier 'superflat'.");
        }

        World.CreateDimension("overworld", DimensionType.Overworld, generatorType);
    }

    public void Start()
    {
        _runCancellation = new CancellationTokenSource();
        _networkLoopTask = Task.Run(async () =>
        {
            await _raknet.Start();
        }, _runCancellation.Token);

        _tickCancellation = new CancellationTokenSource();
        _tickLoopTask = Task.Run(async () =>
        {
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(50));
            while (await timer.WaitForNextTickAsync(_tickCancellation.Token))
            {
                Tick();
            }
        }, _tickCancellation.Token);

        _raknet.OnMessage += Network.HandlePacket;
        _raknet.OnDisconnected += connection =>
        {
            try
            {
                Network.HandleDisconnected(connection);
            }
            catch (Exception exception)
            {
                Logger.Warn($"Unhandled disconnect error: {exception}");
            }
        };

        Logger.Info("Basalt listening on 0.0.0.0:19132");
    }

    public void Stop()
    {
        CancellationTokenSource? runCancellation = _runCancellation;
        Task? networkLoopTask = _networkLoopTask;
        _runCancellation = null;
        _networkLoopTask = null;

        CancellationTokenSource? cancellation = _tickCancellation;
        Task? tickLoopTask = _tickLoopTask;
        _tickCancellation = null;
        _tickLoopTask = null;

        if (runCancellation is null && cancellation is null)
        {
            return;
        }

        runCancellation?.Cancel();
        cancellation?.Cancel();

        try
        {
            networkLoopTask?.Wait(250);
            tickLoopTask?.Wait();
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(static inner => inner is TaskCanceledException))
        { }
        finally
        {
            runCancellation?.Dispose();
            cancellation?.Dispose();
        }
        Logger.Info("Basalt successfully stopped.");
    }

    public WorldInstance CreateWorld(string name, string providerIdentifier, params object[] providerArgs)
    {
        if (string.IsNullOrWhiteSpace(providerIdentifier))
        {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        if (!_providerRegistry.TryGetValue(providerIdentifier, out Type? providerType))
        {
            throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.");
        }

        object? providerInstance = Activator.CreateInstance(providerType, providerArgs);
        if (providerInstance is not WorldProvider provider)
        {
            throw new InvalidOperationException($"Could not construct provider '{providerType.FullName}'.");
        }

        WorldInstance world = new(name, provider);
        world.Server = this;
        return world;
    }

    public void RegisterProvider<TProvider>(string identifier) where TProvider : WorldProvider
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(identifier));
        }

        _providerRegistry[identifier] = typeof(TProvider);
    }

    public void RegisterGenerator<TGenerator>(string identifier) where TGenerator : Generator
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Generator identifier cannot be empty.", nameof(identifier));
        }

        _generatorRegistry[identifier] = typeof(TGenerator);
    }

    public void Broadcast(Dimension dimension, DataPacket packet, BroadcastOptions? options = null)
    {
        BroadcastOptions resolved = options ?? new BroadcastOptions();
        float radiusSquared = resolved.Radius * resolved.Radius;

        foreach ((NetworkConnection connection, Player player) in Players)
        {
            if (player.Dimension != dimension)
            {
                continue;
            }

            if (resolved.Except is not null && resolved.Except.Contains(player))
            {
                continue;
            }

            if (resolved.Center.HasValue)
            {
                Vec3f playerPosition = player.Position;
                Vec3f centerPosition = resolved.Center.Value;
                float dx = playerPosition.X - centerPosition.X;
                float dy = playerPosition.Y - centerPosition.Y;
                float dz = playerPosition.Z - centerPosition.Z;
                float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }
            }

            Network.SendPacket(connection, packet);
        }
    }

    private void Tick()
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        _raknet.Tick();
        World.Tick();

        long endTimestamp = Stopwatch.GetTimestamp();
        ((Tickable)World).TickWork = (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}
