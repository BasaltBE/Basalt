namespace Basalt.Core;

using System.Diagnostics;
using Basalt.Core.Commands;
using Basalt.Core.Commands.Vanilla;
using Basalt.Core.Network;
using Basalt.Core.Plugins;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Core.Events;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;

using PlayerInstance = Player.Player;
using WorldInstance = Worlds.World;

public sealed class Server
{
    private const ulong TpsUpdateIntervalTicks = 20;
    private static readonly long TickDurationTicks = (long)(50.0 / 1000.0 * Stopwatch.Frequency);
    private static readonly long SpinThresholdTicks = (long)(2.0 / 1000.0 * Stopwatch.Frequency);

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
    private readonly Dictionary<ServerEvent, List<Delegate>> _signalHandlers = [];
    public readonly Dictionary<NetworkConnection, PlayerInstance> Players = new();
    public CommandRegistry Commands = new();
    public PluginManager Plugins { get; }
    public NetworkHandler Network { get; }
    public Properties Properties { get; }
    public TaskWorkerPool WorkerPool { get; private set; } = null!;
    public TaskScheduler Scheduler { get; private set; } = null!;
    public IEnumerable<WorldInstance> Worlds => _worlds.Values;

    public string DefaultWorldIdentifier { get; }

    /// <summary>
    /// Ticks per second on average
    /// </summary>
    public double Tps { get; private set; } = 20.0;

    public Server(Properties? properties = null)
    {
        Properties = properties ?? new Properties();
        _raknet = new NetworkServer(new RaknetServerOptions(MaxMtu: Properties.Mtu, Port: Properties.Port));
        Network = new NetworkHandler(this);
        Plugins = new PluginManager(this);
        WorkerPool = new TaskWorkerPool(Properties.WorkerThreads);
        Scheduler = new TaskScheduler(WorkerPool);

        RegisterProvider<LevelDbProvider>("leveldb");
        RegisterProvider<InMemoryProvider>("memory");
        RegisterGenerator<VoidGenerator>("void");
        RegisterGenerator<SuperFlatGenerator>("superflat");

        
#pragma warning disable IL2026
        Plugins.LoadAll(Properties.PluginsDirectory);
#pragma warning restore IL2026

        DefaultWorldIdentifier = Properties.DefaultWorldIdentifier;
        WorldInstance defaultWorld = Properties.WorldProvider.Equals("memory", StringComparison.OrdinalIgnoreCase)
            ? LoadWorld(DefaultWorldIdentifier, Properties.WorldProvider) ?? CreateWorld(DefaultWorldIdentifier, Properties.WorldProvider)
            : LoadWorld(DefaultWorldIdentifier, Properties.WorldProvider, Properties.WorldPath) ?? CreateWorld(DefaultWorldIdentifier, Properties.WorldProvider, Properties.WorldPath);

        if (!_generatorRegistry.TryGetValue("superflat", out Type? generatorType))
        {
            throw new KeyNotFoundException("No generator registered with identifier 'superflat'.");
        }

        if (defaultWorld.GetDimension("overworld") is null)
        {
            defaultWorld.CreateDimension("overworld", DimensionType.Overworld, generatorType);
        }
        defaultWorld.ConfigurePersistence(Properties.WorldPath);

        DefaultCommands.Register(Commands);
        Crafting.CraftingLoader.Load();
    }

    public void Start()
    {
        Plugins.StartAll();
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
            long nextTick = Stopwatch.GetTimestamp() + TickDurationTicks;

            while (!token.IsCancellationRequested)
            {
                long remaining = nextTick - Stopwatch.GetTimestamp();

                if (remaining > SpinThresholdTicks)
                {
                    long sleepTicks = remaining - SpinThresholdTicks;
                    Thread.Sleep(TimeSpan.FromSeconds((double)sleepTicks / Stopwatch.Frequency));
                }

                while (Stopwatch.GetTimestamp() < nextTick)
                {
                    Thread.SpinWait(1);
                }

                Tick();
                nextTick += TickDurationTicks;
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

        Emit(new ServerStartSignal());
        Logger.Info($"Basalt listening on 0.0.0.0:{Properties.Port}");
    }

    public void On<TSignal>(ServerEvent @event, Action<TSignal> handler) where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_signalHandlers.TryGetValue(@event, out List<Delegate>? handlers))
        {
            handlers = [];
            _signalHandlers[@event] = handlers;
        }

        handlers.Add(handler);
    }

    public void Emit(ServerEvent @event, ISignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (!_signalHandlers.TryGetValue(@event, out List<Delegate>? handlers))
        {
            return;
        }

        for (int i = 0; i < handlers.Count; i++)
        {
            Delegate handler = handlers[i];
            Type? signalType = handler.Method.GetParameters().FirstOrDefault()?.ParameterType;
            if (signalType is null || !signalType.IsInstanceOfType(signal))
            {
                continue;
            }

            handler.DynamicInvoke(signal);
        }
    }

    public void Emit(ISignal signal)
    {
        Emit(signal.Event, signal);
    }

    public void Stop()
    {
        Plugins.DisableAll();
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

        foreach (PlayerInstance player in Players.Values.ToArray())
        {
            try
            {
                player.Disconnect("Server closed.");
            }
            catch (Exception exception)
            {
                Logger.Warn($"Unhandled disconnect error during shutdown: {exception}");
            }
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
            WorkerPool.Dispose();
        }
        Logger.Info("Basalt successfully stopped.");
    }

    public WorldInstance CreateWorld(string name, string providerIdentifier, params object[] providerArgs)
    {
        if (_worlds.ContainsKey(name))
        {
            throw new InvalidOperationException($"World '{name}' already exists.");
        }

        if (string.IsNullOrWhiteSpace(providerIdentifier))
        {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        if (!_providerRegistry.TryGetValue(providerIdentifier, out Type? providerType))
        {
            throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.");
        }

        if (providerArgs.Length == 0 && providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase))
        {
            providerArgs = [Path.Combine("worlds", name)];
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

    public WorldInstance? LoadWorld(string name, string providerIdentifier, params object[] providerArgs)
    {
        if (string.IsNullOrWhiteSpace(providerIdentifier))
        {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        if (!_providerRegistry.TryGetValue(providerIdentifier, out Type? providerType))
        {
            throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.");
        }

        if (providerIdentifier.Equals("memory", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (providerArgs.Length == 0 && providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase))
        {
            providerArgs = [Path.Combine("worlds", name)];
        }

        if (providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase))
        {
            string path = providerArgs.Length > 0 ? providerArgs[0] as string ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())
            {
                return null;
            }
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

    public bool UnloadWorld(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("World identifier cannot be empty.", nameof(identifier));
        }

        if (identifier.Equals(DefaultWorldIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot unload the default world.");
        }

        if (!_worlds.Remove(identifier, out WorldInstance? world))
        {
            return false;
        }

        world.Server = null;
        world.Dispose();
        return true;
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
        using var _ = Profiler.BeginZone("Server.Tick");
        long startTimestamp = Stopwatch.GetTimestamp();

        using (Profiler.BeginZone("Raknet.Tick"))
        {
            _raknet.Tick();
        }

        using (Profiler.BeginZone("TaskScheduler.Tick"))
        {
            Scheduler.Tick(GetWorld().TickValue);
        }

        foreach (WorldInstance world in _worlds.Values.ToArray())
        {
            using var worldZone = Profiler.BeginZone($"World.Tick({world.Name})");
            long worldStartTimestamp = Stopwatch.GetTimestamp();
            world.Tick();
            long worldEndTimestamp = Stopwatch.GetTimestamp();
            ((Tickable)world).TickWork = (worldEndTimestamp - worldStartTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        long endTimestamp = Stopwatch.GetTimestamp();
        UpdateTps(endTimestamp);
        Profiler.FrameMark();
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


    public void Broadcast(DataPacket packet, params PlayerInstance[]? exclude)
    {
        foreach ((NetworkConnection connection, PlayerInstance player) in Players)
        {
            if (exclude is not null)
            {
                bool skipped = false;
                for (int i = 0; i < exclude.Length; i++)
                {
                    if (ReferenceEquals(exclude[i], player))
                    {
                        skipped = true;
                        break;
                    }
                }

                if (skipped)
                {
                    continue;
                }
            }

            Network.SendPacket(connection, packet);
        }
    }
}







