using Basalt.Commands;
using Basalt.Network;
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
    /// TODO! Adjust cause of faking windows
    /// </summary>
    private const ulong TpsUpdateIntervalTicks = 20;
    private const double TickIntervalMs = 50.0;
    private const double SpinThresholdMs = 16.0;

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
    private readonly Dictionary<string, WorldInstance> _worlds = new(StringComparer.OrdinalIgnoreCase);
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
    private long _lastTpsTimestamp;
    private ulong _lastTpsTick;
    /// <summary>
    /// Registry for players
    /// </summary>
    public readonly Dictionary<NetworkConnection, Player> Players = new();
    /// <summary>
    /// Registry for commands
    /// </summary>
    public CommandRegistry Commands = new();
    /// <summary>
    /// Network handler for processing minecraft packets and packet handlers
    /// </summary>
    public NetworkHandler Network { get; }
    /// <summary>
    /// Server options
    /// </summary>
    public ServerOptions Options { get; }
    public IEnumerable<WorldInstance> Worlds => _worlds.Values;

    public string DefaultWorldIdentifier { get; }

    /// <summary>
    /// Ticks per second on average
    /// </summary>
    public double Tps { get; private set; } = 20.0;

    public Server(ServerOptions options = default)
    {
        Options = options == default ? new ServerOptions() : options;
        _raknet = new NetworkServer();
        Network = new NetworkHandler(this);

        RegisterProvider<LevelDbProvider>("leveldb");
        RegisterProvider<InMemoryProvider>("memory");
        RegisterGenerator<VoidGenerator>("void");
        RegisterGenerator<SuperFlatGenerator>("superflat");

        DefaultWorldIdentifier = Options.DefaultWorldIdentifier;
        WorldInstance defaultWorld = Options.WorldProvider.Equals("memory", StringComparison.OrdinalIgnoreCase)
            ? CreateWorld(DefaultWorldIdentifier, Options.WorldProvider)
            : CreateWorld(DefaultWorldIdentifier, Options.WorldProvider, Options.WorldPath);

        if (!_generatorRegistry.TryGetValue("superflat", out Type? generatorType))
        {
            throw new KeyNotFoundException("No generator registered with identifier 'superflat'.");
        }

        defaultWorld.CreateDimension("overworld", DimensionType.Overworld, generatorType);

        Commands.RegisterDefaultCommands();
    }

    public void Start()
    {
        Commands.CacheAvailableCommands();
        _lastTpsTimestamp = Stopwatch.GetTimestamp();
        _lastTpsTick = GetWorld().TickValue;

        _runCancellation = new CancellationTokenSource();
        _networkLoopTask = Task.Run(async () =>
        {
            await _raknet.Start();
        }, _runCancellation.Token);

        CancellationTokenSource tickCancellation = new();
        _tickCancellation = tickCancellation;
        _tickLoopTask = Task.Run(() =>
        {
            CancellationToken token = tickCancellation.Token;
            while (!token.IsCancellationRequested)
            {
                long tickStartTimestamp = Stopwatch.GetTimestamp();
                Tick();

                long tickDeadlineTimestamp = tickStartTimestamp + (long)(TickIntervalMs * Stopwatch.Frequency / 1000.0);
                double remainingMs = GRTM(tickDeadlineTimestamp, Stopwatch.GetTimestamp());
                if (remainingMs <= 0)
                {
                    continue;
                }

                while (remainingMs > SpinThresholdMs)
                {
                    Thread.Sleep(1);
                    remainingMs = GRTM(tickDeadlineTimestamp, Stopwatch.GetTimestamp());
                    if (remainingMs <= 0)
                    {
                        break;
                    }
                }

                while (Stopwatch.GetTimestamp() < tickDeadlineTimestamp)
                {
                    Thread.SpinWait(1);
                }
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
        _worlds[name] = world;
        return world;
    }

    public WorldInstance GetWorld()
    {
        return GetWorld(DefaultWorldIdentifier);
    }

    public WorldInstance GetWorld(string identifier)
    {
        if (_worlds.TryGetValue(identifier, out WorldInstance? world))
        {
            return world;
        }

        throw new KeyNotFoundException($"World '{identifier}' was not found.");
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

    public void Tick()
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        _raknet.Tick();
        foreach (WorldInstance world in _worlds.Values.ToArray())
        {
            long worldStartTimestamp = Stopwatch.GetTimestamp();
            world.Tick();
            long worldEndTimestamp = Stopwatch.GetTimestamp();
            ((Tickable)world).TickWork = (worldEndTimestamp - worldStartTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        long endTimestamp = Stopwatch.GetTimestamp();
        UpdateTps(endTimestamp);
    }

    public void UpdateTps(long timestamp)
    {
        if (_lastTpsTimestamp == 0)
        {
            _lastTpsTimestamp = timestamp;
            _lastTpsTick = GetWorld().TickValue;
            return;
        }

        ulong tickDelta = GetWorld().TickValue - _lastTpsTick;
        if (tickDelta < TpsUpdateIntervalTicks)
        {
            return;
        }

        long timestampDelta = timestamp - _lastTpsTimestamp;
        if (tickDelta == 0 || timestampDelta <= 0)
        {
            return;
        }

        double elapsedSeconds = (double)timestampDelta / Stopwatch.Frequency;
        double currentTps = Math.Min(20.0, tickDelta / elapsedSeconds);
        Tps = Tps == 0 ? currentTps : Tps + ((currentTps - Tps) * 0.2);
        _lastTpsTimestamp = timestamp;
        _lastTpsTick = GetWorld().TickValue;
    }

    private static double GRTM(long deadlineTimestamp, long timestamp)
    {
        return (deadlineTimestamp - timestamp) * 1000.0 / Stopwatch.Frequency;
    }
}
