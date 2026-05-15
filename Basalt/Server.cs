using Basalt.Network;
using Basalt.Protocol.Types;
using Basalt.Protocol.Enums;
using Basalt.RakNet;
using Basalt.World.Dimension;
using Basalt.World.Dimension.Provider;
using Basalt.World.Dimension.Generation;
using System.Diagnostics;
using WorldInstance = Basalt.World.World;

namespace Basalt.Core;

public sealed class Server
{
    private readonly NetworkServer _raknet;
    private readonly NetworkHandler _network;
    private CancellationTokenSource? _runCancellation;
    private Task? _networkLoopTask;
    private CancellationTokenSource? _tickCancellation;
    private Task? _tickLoopTask;

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
        _raknet.OnDisconnected += connection =>
        {
            try
            {
                _network.HandleDisconnected(connection);
            }
            catch (Exception exception)
            {
                Logger.Warn($"Unhandled disconnect error: {exception}");
            }
        };

        World = CreateDefaultWorld();
        AttachWorldBroadcasts(World);
    }

    public void Start()
    {
        _runCancellation = new CancellationTokenSource();
        _networkLoopTask = Task.Run(async () =>
        {
            try
            {
                await _raknet.Start();
            }
            catch
            {
            }
        }, _runCancellation.Token);

        _tickCancellation = new CancellationTokenSource();
        _tickLoopTask = RunTickLoopAsync(_tickCancellation.Token);

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
        {
        }
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

    private void AttachWorldBroadcasts(WorldInstance world)
    {
        foreach (var dimension in world.Dimensions)
        {
            dimension.PacketBroadcaster = (packet, options) =>
            {
                float radiusSquared = options.Radius * options.Radius;

                foreach ((NetworkConnection connection, Player player) in Players)
                {
                    if (player.Dimension != dimension)
                    {
                        continue;
                    }

                    if (options.Except is not null && options.Except.Contains(player))
                    {
                        continue;
                    }

                    if (options.Center.HasValue)
                    {
                        Vec3f playerPosition = player.Position;
                        Vec3f centerPosition = options.Center.Value;
                        float dx = playerPosition.X - centerPosition.X;
                        float dy = playerPosition.Y - centerPosition.Y;
                        float dz = playerPosition.Z - centerPosition.Z;
                        float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                        if (distanceSquared > radiusSquared)
                        {
                            continue;
                        }
                    }

                    _network.SendPacket(connection, packet);
                }
            };
        }
    }

    private async Task RunTickLoopAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            Tick();
        }
    }

    private void Tick()
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        _raknet.Tick();
        World.Tick();
        ulong tick = World.CurrentTick;


        //Players and entities tick seperatly
        foreach (KeyValuePair<NetworkConnection, Player> entry in Players)
        {
            entry.Value.Tick(tick, 1);
        }

        long endTimestamp = Stopwatch.GetTimestamp();
        World.LastTickWorkMs = (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}
