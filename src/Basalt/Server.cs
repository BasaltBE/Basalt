namespace Basalt.Core;

using System.Collections.Concurrent;
using System.Diagnostics;
using Basalt.Core.Commands;
using Basalt.Core.Commands.Vanilla;
using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Item;
using Basalt.Core.Network;
using Basalt.Core.Network.Nethernet;
using Basalt.Core.Plugins;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.Core.Tasks;
using Basalt.Core.Events;
using Basalt.Core.Enums;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;

using Basalt.Core.Player;
using PlayerInstance = Player.Player;
using WorldInstance = Worlds.World;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Rcon;

public sealed class Server {
    private const double AdaptiveHotWorldMilliseconds = 2.0;
    private const ulong TpsUpdateIntervalTicks = 20;
    private const ulong AutoSaveIntervalTicks = 6000;
    private const int AutoSaveChunksPerTick = 2;
    private static readonly long TickDurationTicks = (long)(50.0 / 1000.0 * Stopwatch.Frequency);
    private static readonly long SpinThresholdTicks = (long)(2.0 / 1000.0 * Stopwatch.Frequency);

    private readonly NetherNetServerTransport _nethernet;
    private readonly RconServer? _rcon;
    private readonly Dictionary<string, Type> _generatorRegistry = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _providerRegistry = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginContainer> _generatorOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginContainer> _providerOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorldInstance> _worlds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _worldsLock = new();
    private WorldInstance[] _worldsSnapshot = [];
    private readonly Queue<WorldInstance> _autoSaveWorlds = [];
    private readonly List<WorldGroupTickTask> _worldGroupTasks = [];
    private List<WorldInstance>[] _worldGroups = [];
    private readonly ConcurrentQueue<string> _pendingWorldUnloads = new();
    private readonly ConcurrentDictionary<PlayerInstance, PendingPlayerTransfer> _pendingPlayerTransfers = new();
    private int _pendingTickGroups = -1;
    private CancellationTokenSource? _networkCancellation;
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
    private ulong _lastAutoSaveTick;
    private double _tickWorkTotal;
    private double _tickWorkMaximum;
    private int _tickWorkSamples;
    private long _tickAllocatedBytesTotal;
    private double _tps = 20.0;
    private double _tickWork;
    private double _tickWorkAverage;
    private double _tickWorkMaximumPublished;
    private TimeSpan _startupElapsed;
    private readonly Dictionary<ServerEvent, List<SignalHandler>> _signalHandlers = new();
    private readonly Dictionary<ServerEvent, SignalHandler[]> _signalHandlerSnapshots = new();
    public ConcurrentDictionary<NetworkConnection, PlayerInstance> Players { get; } = new();
    private PlayerInstance[] _playersSnapshot = [];
    private int _playersSnapshotDirty = 1;
    public CommandRegistry Commands { get; } = new();
    public PermissionStore PermissionStore { get; }
    public PlayerDataStore PlayerData { get; }
    public BanStore Bans { get; }
    public PluginManager Plugins { get; }
    public NetworkHandler Network { get; }
    public Properties Properties { get; }
    public ResourcePackManager ResourcePacks { get; } = new();
    public TaskWorkerPool TickWorkerPool { get; private set; } = null!;
    public TaskWorkerPool BackgroundWorkerPool { get; private set; } = null!;
    public TaskWorkerPool WorkerPool => BackgroundWorkerPool;
    public TaskScheduler Scheduler { get; private set; } = null!;
    public IEnumerable<WorldInstance> Worlds => Volatile.Read(ref _worldsSnapshot);

    public IEnumerable<PlayerInstance> GetPlayers() {
        foreach (WorldInstance world in Worlds) {
            foreach (PlayerInstance player in world.GetPlayers()) {
                yield return player;
            }
        }
    }

    public PlayerInstance[] GetPlayersSnapshot() => [.. Volatile.Read(ref _playersSnapshot)];

    internal IReadOnlyList<PlayerInstance> CurrentPlayersSnapshot => Volatile.Read(ref _playersSnapshot);

    private void RefreshPlayersSnapshot() {
        if (Interlocked.Exchange(ref _playersSnapshotDirty, 0) == 0) {
            return;
        }

        Volatile.Write(ref _playersSnapshot, [.. GetPlayers()]);
    }

    internal void MarkPlayersSnapshotDirty() {
        Volatile.Write(ref _playersSnapshotDirty, 1);
    }

    private void RefreshWorldsSnapshot() {
        lock (_worldsLock) {
            Volatile.Write(ref _worldsSnapshot, [.. _worlds.Values]);
        }
    }

    internal void QueuePlayerTransfer(
        PlayerInstance player,
        Dimension source,
        Dimension target,
        Vec3 position) {
        _pendingPlayerTransfers[player] = new PendingPlayerTransfer(source, target, position);
    }

    internal void ApplyPlayerTransfers() {
        foreach ((PlayerInstance player, PendingPlayerTransfer transfer) in _pendingPlayerTransfers) {
            if (!_pendingPlayerTransfers.TryRemove(player, out PendingPlayerTransfer pending) ||
                player.Dimension != pending.Source ||
                player.PendingDespawn) {
                continue;
            }

            player.ApplyQueuedTeleport(pending.Position, pending.Target);
            MarkPlayersSnapshotDirty();
        }
    }

    public string DefaultWorldIdentifier { get; }

    /// <summary>
    /// Ticks per second on average.
    /// </summary>
    public double Tps => Volatile.Read(ref _tps);

    /// <summary>
    /// Milliseconds the last server tick took.
    /// </summary>
    public double TickWork {
        get => Volatile.Read(ref _tickWork);
        private set => Volatile.Write(ref _tickWork, value);
    }

    public double TickWorkAverage => Volatile.Read(ref _tickWorkAverage);

    public double TickWorkMaximum => Volatile.Read(ref _tickWorkMaximumPublished);
    internal int LastAdaptiveGroupCount { get; private set; }

    public long TickAllocatedBytes { get; private set; }

    public double TickAllocatedBytesAverage { get; private set; }

    public Server(Properties? properties = null) {
        long startTimestamp = Stopwatch.GetTimestamp();
        Properties = properties ?? new Properties();
        if (Properties.RconPort != 0 && Properties.RconPassword.Length > 0) {
            _rcon = new RconServer(this, Properties.RconPort, Properties.RconPassword);
        }
        Network = new NetworkHandler(this);
        _nethernet = new NetherNetServerTransport(Network, Properties.Port, Properties.Ipv6Port);
        PermissionStore = new PermissionStore();
        PlayerData = new PlayerDataStore(Properties.PlayerDataPath);
        Bans = new BanStore("banned-players.json");
        Plugins = new PluginManager(this);
        Commands.PluginOwnerProvider = () => Plugins.CurrentRegistrationPlugin;
        Commands.PluginScopeProvider = Plugins.EnterRegistrationScope;
        Commands.PluginErrorHandler = (plugin, callback, exception) =>
            Plugins.RecordRuntimeFailure(plugin, callback, exception);
        int tickWorkerCount = Properties.TickWorkerThreads > 0
            ? Properties.TickWorkerThreads
            : Properties.WorkerThreads > 0
                ? Properties.WorkerThreads
                : Math.Min(Math.Max(1, Environment.ProcessorCount - 2), 12);
        int backgroundWorkerCount = Properties.BackgroundWorkerThreads > 0
            ? Properties.BackgroundWorkerThreads
            : Math.Clamp(Environment.ProcessorCount / 4, 1, 4);
        TickWorkerPool = new TaskWorkerPool(WorkerKind.Tick, Math.Max(1, tickWorkerCount));
        BackgroundWorkerPool = new TaskWorkerPool(WorkerKind.Background, Math.Max(1, backgroundWorkerCount));
        Scheduler = new TaskScheduler(
            BackgroundWorkerPool,
            () => Plugins.CurrentRegistrationPlugin,
            Plugins.ConfigureTask);

        DefaultCommands.Register(Commands);

        RegisterProvider<LevelDbProvider>("leveldb");
        RegisterProvider<InMemoryProvider>("memory");
        RegisterGenerator<VoidGenerator>("void");
        RegisterGenerator<SuperFlatGenerator>("superflat");


        Plugins.LoadAll(Properties.PluginsDirectory);

        ResourcePacks.Load(Properties.ResourcePacksPath);

        DefaultWorldIdentifier = Properties.DefaultWorldIdentifier;
        string defaultWorldPath = Path.Combine(Properties.WorldPath, DefaultWorldIdentifier);
        WorldInstance defaultWorld = Properties.WorldProvider.Equals("memory", StringComparison.OrdinalIgnoreCase)
            ? LoadWorld(DefaultWorldIdentifier, Properties.WorldProvider)
                ?? CreateWorld(DefaultWorldIdentifier, Properties.WorldProvider)
            : LoadWorld(DefaultWorldIdentifier, Properties.WorldProvider, defaultWorldPath)
                ?? CreateWorld(DefaultWorldIdentifier, Properties.WorldProvider, defaultWorldPath);

        if (!_generatorRegistry.TryGetValue("superflat", out Type? generatorType)) {
            throw new KeyNotFoundException("No generator registered with identifier 'superflat'.");
        }

        if (defaultWorld.GetDimension("overworld") is null) {
            defaultWorld.CreateDimension("overworld", DimensionId.Overworld, generatorType);
        }
        WorldInstance.ConfigurePersistence(defaultWorldPath);

        Crafting.CraftingLoader.Load();

        _startupElapsed = Stopwatch.GetElapsedTime(startTimestamp);
    }

    public void Start() {
        Plugins.StartAll();
        _rcon?.Start();

        _ = Item.ItemPalette.GetItemRegistryPayload();
        _ = Item.ItemPalette.GetCreativeContentPayload();
        _ = Crafting.CraftingRegistry.Instance.GetCraftingDataPayload();

        _lastTpsTimestamp = Stopwatch.GetTimestamp();
        _lastTpsTick = GetWorld().TickValue;

        CancellationTokenSource networkCancellation = new();
        _networkCancellation = networkCancellation;
        Network.Start(networkCancellation.Token);
        _nethernet.Start(networkCancellation.Token);

        CancellationTokenSource tickCancellation = new();
        _tickCancellation = tickCancellation;
        _tickLoopTask = Task.Run(() => {
            Profiler.SetThreadName("Main");
            CancellationToken token = tickCancellation.Token;
            long nextTick = Stopwatch.GetTimestamp() + TickDurationTicks;

            while (!token.IsCancellationRequested) {
                long remaining = nextTick - Stopwatch.GetTimestamp();

                if (remaining > SpinThresholdTicks) {
                    long sleepTicks = remaining - SpinThresholdTicks;
                    Thread.Sleep(TimeSpan.FromSeconds((double)sleepTicks / Stopwatch.Frequency));
                }

                while (Stopwatch.GetTimestamp() < nextTick) {
                    Thread.SpinWait(1);
                }

                try {
                    Tick();
                }
                catch (Exception exception) {
                    Logger.Error($"Unhandled tick error: {exception}");
                }

                nextTick += TickDurationTicks;
            }
        }, _tickCancellation.Token);

        Emit(new ServerStartSignal());
        TimeSpan registryElapsed = BlockPalette.LoadElapsed +
            ItemPalette.LoadElapsed +
            EntityPalette.LoadElapsed;
        TimeSpan processStartupElapsed = registryElapsed + _startupElapsed;
        Logger.Info($"Protocol JSON data loaded and parsed in {registryElapsed.TotalMilliseconds:0.00}ms.");
        Logger.Info(
            $"Basalt NetherNet signaling on IPv4 port {Properties.Port} and IPv6 port {Properties.Ipv6Port} " +
            $"startup~{processStartupElapsed.TotalMilliseconds:0}ms,");
    }

    public void On<TSignal>(ServerEvent @event, Action<TSignal> handler) where TSignal : ISignal {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_signalHandlers.TryGetValue(@event, out List<SignalHandler>? handlers)) {
            handlers = [];
            _signalHandlers[@event] = handlers;
        }

        handlers.Add(new TypedSignalHandler<TSignal>(handler, Plugins.CurrentRegistrationPlugin));
        _signalHandlerSnapshots[@event] = [.. handlers];
    }

    public void Emit(ServerEvent @event, ISignal signal) {
        ArgumentNullException.ThrowIfNull(signal);
        if (!_signalHandlerSnapshots.TryGetValue(@event, out SignalHandler[]? handlers)) {
            return;
        }

        for (int i = 0; i < handlers.Length; i++) {
            SignalHandler handler = handlers[i];
            try {
                using (Plugins.EnterRegistrationScope(handler.Plugin))
                    handler.Invoke(signal);
            }
            catch (Exception exception) {
                Plugins.RecordRuntimeFailure(handler.Plugin, @event, exception);
            }
        }
    }

    public void Emit(ISignal signal) {
        Emit(signal.Event, signal);
    }

    public bool HasListeners(ServerEvent @event) {
        return _signalHandlerSnapshots.TryGetValue(@event, out SignalHandler[]? handlers) && handlers.Length > 0;
    }

    public void Stop() {
        _rcon?.Stop();
        Plugins.DisableAll();
        CancellationTokenSource? networkCancellation = _networkCancellation;
        _networkCancellation = null;
        CancellationTokenSource? cancellation = _tickCancellation;
        Task? tickLoopTask = _tickLoopTask;
        _tickCancellation = null;
        _tickLoopTask = null;

        if (networkCancellation is null && cancellation is null) {
            return;
        }

        SavePlayers();

        foreach (PlayerInstance player in Players.Values.ToArray()) {
            try {
                player.Disconnect("Server closed.", true);
            }
            catch (Exception exception) {
                Logger.Warn($"Unhandled disconnect error during shutdown: {exception}");
            }
        }

        networkCancellation?.Cancel();
        cancellation?.Cancel();

        _nethernet.Dispose();

        try {
            tickLoopTask?.Wait();
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(static inner => inner is TaskCanceledException)) { }
        finally {
            networkCancellation?.Dispose();
            cancellation?.Dispose();
            TickWorkerPool.Dispose();
            BackgroundWorkerPool.Dispose();
            Network.Stop();
            Network.Dispose();
        }

        foreach (WorldInstance world in Worlds) {
            world.Dispose();
        }

        Logger.Info("Basalt successfully stopped.");
    }

    public WorldInstance CreateWorld(string name, string providerIdentifier, params object[] providerArgs) {
        if (Volatile.Read(ref _ticking) != 0 && !IsCoordinatorThread) {
            throw new InvalidOperationException("World creation must run on the server coordinator.");
        }

        lock (_worldsLock) {
            if (_worlds.ContainsKey(name)) {
                throw new InvalidOperationException($"World '{name}' already exists.");
            }
        }

        if (string.IsNullOrWhiteSpace(providerIdentifier)) {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        if (!_providerRegistry.TryGetValue(providerIdentifier, out Type? providerType)) {
            throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.");
        }

        if (providerArgs.Length == 0 && providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase)) {
            providerArgs = [Path.Combine("worlds", name)];
        }

        object? providerInstance = Activator.CreateInstance(providerType, providerArgs);
        if (providerInstance is not WorldProvider provider) {
            throw new InvalidOperationException($"Could not construct provider '{providerType.FullName}'.");
        }

        WorldInstance world = new(name, provider);
        world.Server = this;
        lock (_worldsLock) {
            if (_worlds.ContainsKey(name)) {
                world.Dispose();
                throw new InvalidOperationException($"World '{name}' already exists.");
            }

            _worlds[name] = world;
        }
        RefreshWorldsSnapshot();
        return world;
    }

    public WorldInstance? LoadWorld(string name, string providerIdentifier, params object[] providerArgs) {
        if (Volatile.Read(ref _ticking) != 0 && !IsCoordinatorThread) {
            throw new InvalidOperationException("World loading must run on the server coordinator.");
        }

        if (string.IsNullOrWhiteSpace(providerIdentifier)) {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(providerIdentifier));
        }

        if (!_providerRegistry.TryGetValue(providerIdentifier, out Type? providerType)) {
            throw new KeyNotFoundException($"Unknown provider identifier '{providerIdentifier}'.");
        }

        if (providerIdentifier.Equals("memory", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        if (providerArgs.Length == 0 && providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase)) {
            providerArgs = [Path.Combine("worlds", name)];
        }

        if (providerIdentifier.Equals("leveldb", StringComparison.OrdinalIgnoreCase)) {
            string path = providerArgs.Length > 0 ? providerArgs[0] as string ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any()) {
                return null;
            }
        }

        object? providerInstance = Activator.CreateInstance(providerType, providerArgs);
        if (providerInstance is not WorldProvider provider) {
            throw new InvalidOperationException($"Could not construct provider '{providerType.FullName}'.");
        }

        WorldInstance world = new(name, provider);
        world.Server = this;
        lock (_worldsLock) {
            if (_worlds.ContainsKey(name)) {
                world.Dispose();
                throw new InvalidOperationException($"World '{name}' already exists.");
            }

            _worlds[name] = world;
        }
        RefreshWorldsSnapshot();
        return world;
    }

    public bool UnloadWorld(string identifier) {
        if (string.IsNullOrWhiteSpace(identifier)) {
            throw new ArgumentException("World identifier cannot be empty.", nameof(identifier));
        }

        if (identifier.Equals(DefaultWorldIdentifier, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Cannot unload the default world.");
        }

        lock (_worldsLock) {
            if (!_worlds.ContainsKey(identifier)) {
                return false;
            }
        }

        if (Volatile.Read(ref _ticking) != 0) {
            _pendingWorldUnloads.Enqueue(identifier);
            return true;
        }

        UnloadWorldNow(identifier);
        return true;
    }

    internal void RemovePluginHandlers(PluginContainer plugin) {
        foreach (List<SignalHandler> handlers in _signalHandlers.Values)
            handlers.RemoveAll(handler => ReferenceEquals(handler.Plugin, plugin));
    }

    internal void RemovePluginRegistrations(PluginContainer plugin) {
        foreach (string identifier in _generatorOwners
            .Where(pair => ReferenceEquals(pair.Value, plugin))
            .Select(pair => pair.Key)
            .ToArray()) {
            _generatorOwners.Remove(identifier);
            _generatorRegistry.Remove(identifier);
        }

        foreach (string identifier in _providerOwners
            .Where(pair => ReferenceEquals(pair.Value, plugin))
            .Select(pair => pair.Key)
            .ToArray()) {
            _providerOwners.Remove(identifier);
            _providerRegistry.Remove(identifier);
        }
    }

    private void UnloadWorldNow(string identifier) {
        WorldInstance? world;
        lock (_worldsLock) {
            if (!_worlds.Remove(identifier, out world)) {
                return;
            }
        }

        world.Server = null;
        world.Dispose();
        RefreshWorldsSnapshot();
    }

    public WorldInstance GetWorld() {
        return GetWorld(DefaultWorldIdentifier);
    }

    public WorldInstance GetWorld(string identifier) {
        lock (_worldsLock) {
            if (_worlds.TryGetValue(identifier, out WorldInstance? world)) {
                return world;
            }
        }

        throw new KeyNotFoundException($"World '{identifier}' was not found.");
    }

    public void SaveAll() {
        SavePlayers();
        foreach (WorldInstance world in Worlds) {
            world.Save();
        }
    }

    public void SavePlayer(PlayerInstance player) {
        PlayerData.Save(player.Xuid, player.Write());
    }

    public void SavePlayers() {
        foreach (PlayerInstance player in Players.Values) {
            SavePlayer(player);
        }
    }

    public void BanPlayer(PlayerInstance player, DateTimeOffset? until = null, string reason = "") {
        StoreBan(player.Xuid, player.Username, until, reason);
        KickPlayer(player, string.IsNullOrWhiteSpace(reason) ? "You are banned from this server." : reason);
    }

    public void BanPlayer(string identifier, DateTimeOffset? until = null, string reason = "") {
        StoreBan(identifier, string.Empty, until, reason);
    }

    public void BanPlayer(string xuid, string username, DateTimeOffset? until = null, string reason = "") {
        StoreBan(xuid, username, until, reason);
    }

    public bool UnBanPlayer(string identifier) {
        return Bans.Remove(identifier);
    }

    public bool UnBanPlayer(PlayerInstance player) {
        return Bans.Remove(player.Xuid) || Bans.Remove(player.Username);
    }

    public bool IsBanned(string identifier) {
        return Bans.IsBanned(identifier, out _);
    }

    public bool IsBanned(PlayerInstance player) {
        return Bans.IsBanned(player.Xuid, player.Username, out _);
    }

    public static void KickPlayer(PlayerInstance player, string reason = "") {
        player.Disconnect(reason, true);
    }

    private void StoreBan(string xuid, string username, DateTimeOffset? until, string reason) {
        Bans.Ban(new BanEntry {
            Identifier = string.IsNullOrEmpty(xuid) ? username : xuid,
            Username = username,
            Xuid = xuid,
            Until = until?.ToUnixTimeSeconds() ?? 0,
            Reason = reason
        });
    }

    public void RegisterProvider<TProvider>(string identifier) where TProvider : WorldProvider {
        if (string.IsNullOrWhiteSpace(identifier)) {
            throw new ArgumentException("Provider identifier cannot be empty.", nameof(identifier));
        }

        _providerRegistry[identifier] = typeof(TProvider);
        if (Plugins.CurrentRegistrationPlugin is { } plugin)
            _providerOwners[identifier] = plugin;
    }

    public void RegisterGenerator<TGenerator>(string identifier) where TGenerator : Generator {
        if (string.IsNullOrWhiteSpace(identifier)) {
            throw new ArgumentException("Generator identifier cannot be empty.", nameof(identifier));
        }

        _generatorRegistry[identifier] = typeof(TGenerator);
        if (Plugins.CurrentRegistrationPlugin is { } plugin)
            _generatorOwners[identifier] = plugin;
    }

    private int _ticking;
    private int _coordinatorThreadId;

    internal bool IsCoordinatorThread => Volatile.Read(ref _coordinatorThreadId) == Environment.CurrentManagedThreadId;

    public void Tick() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Server.Tick") : default;
        if (Interlocked.Exchange(ref _ticking, 1) != 0) {
            return;
        }

        Volatile.Write(ref _coordinatorThreadId, Environment.CurrentManagedThreadId);
        try {
            TickCore();
        }
        finally {
            Volatile.Write(ref _ticking, 0);
            while (_pendingWorldUnloads.TryDequeue(out string? identifier)) {
                UnloadWorldNow(identifier);
            }

            Volatile.Write(ref _coordinatorThreadId, 0);
        }
    }

    private void TickCore() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("Server.Tick") : default;
        long startTimestamp = Stopwatch.GetTimestamp();
        long allocatedStart = GC.GetAllocatedBytesForCurrentThread();

        ApplyPendingTickGroups();

        using (Profiler.Enabled ? Profiler.BeginZone("Server.Network") : default) {
            Network.ProcessIncoming();
        }

        using (Profiler.Enabled ? Profiler.BeginZone("TaskScheduler.Tick") : default) {
            Scheduler.Tick(GetWorld().TickValue);
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Server.Worlds") : default) {
            WorldInstance[] worlds = Volatile.Read(ref _worldsSnapshot);
            switch (Properties.TickMode) {
                case Enums.TickMode.World:
                    TickWorldsParallel(worlds);
                    break;
                case Enums.TickMode.Group:
                    TickWorldGroupsParallel(worlds);
                    break;
                case Enums.TickMode.Adaptive when worlds.Length > 1 && TickWorkerPool.WorkerCount > 1:
                    TickWorldGroupsParallel(worlds, GetAdaptiveGroupCount(worlds));
                    break;
                default:
                    for (int i = 0; i < worlds.Length; i++) {
                        TickWorld(worlds[i]);
                    }
                    break;
            }
        }

        ApplyPlayerTransfers();
        RefreshPlayersSnapshot();

        ulong currentTick = GetWorld().TickValue;
        if (currentTick - _lastAutoSaveTick >= AutoSaveIntervalTicks) {
            _lastAutoSaveTick = currentTick;
            SavePlayers();
            foreach (WorldInstance world in Worlds) {
                if (world.AutoSaving) {
                    continue;
                }

                world.BeginAutoSave();
                _autoSaveWorlds.Enqueue(world);
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Server.AutoSave") : default) {
            int remaining = AutoSaveChunksPerTick;
            while (remaining > 0 && _autoSaveWorlds.TryPeek(out WorldInstance? world)) {
                int processed = world.AutoSave(remaining);
                remaining -= processed;
                if (world.AutoSaving) {
                    break;
                }

                _autoSaveWorlds.Dequeue();
                if (processed == 0) {
                    remaining--;
                }
            }
        }

        long endTimestamp = Stopwatch.GetTimestamp();
        TickWork = (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        TickAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedStart;
        _tickAllocatedBytesTotal += TickAllocatedBytes;
        _tickWorkTotal += TickWork;
        _tickWorkMaximum = Math.Max(_tickWorkMaximum, TickWork);
        _tickWorkSamples++;
        TickAllocatedBytesAverage = (double)_tickAllocatedBytesTotal / _tickWorkSamples;
        UpdateTps(endTimestamp);
        Profiler.FrameMark();
    }

    public void RequestTickGroups(int groupCount) {
        ArgumentOutOfRangeException.ThrowIfNegative(groupCount);
        Volatile.Write(ref _pendingTickGroups, groupCount);
    }

    private void ApplyPendingTickGroups() {
        int groupCount = Interlocked.Exchange(ref _pendingTickGroups, -1);
        if (groupCount >= 0) {
            Properties.TickGroups = groupCount;
        }
    }

    private static void TickWorld(WorldInstance world) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone($"Server.TickWorld({world.Name})") : default;
        long startTimestamp = Stopwatch.GetTimestamp();
        world.Tick();
        ((Tickable)world).TickWork = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    private void TickWorldsParallel(WorldInstance[] worlds) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Server.TickWorldsParallel") : default;
        if (worlds.Length < 2) {
            for (int i = 0; i < worlds.Length; i++) {
                TickWorld(worlds[i]);
            }

            return;
        }

        List<(WorldInstance World, WorldTickTask Task, ManualResetEventSlim Completed)> tasks = [];
        for (int i = 0; i < worlds.Length; i++) {
            WorldInstance world = worlds[i];
            ManualResetEventSlim completed = new();
            WorldTickTask task = new(world, completed);
            if (TickWorkerPool.TryEnqueue(task)) {
                tasks.Add((world, task, completed));
            }
            else {
                completed.Dispose();
                TickWorld(world);
            }
        }

        for (int i = 0; i < tasks.Count; i++) {
            (WorldInstance world, WorldTickTask task, ManualResetEventSlim completed) = tasks[i];
            completed.Wait();
            completed.Dispose();
            ((Tickable)world).TickWork = task.ElapsedMilliseconds;
            if (task.Error is not null) {
                Logger.Warn($"World tick failed: {task.Error}");
            }
        }
    }

    private void TickWorldGroupsParallel(WorldInstance[] worlds, int? requestedGroupCount = null) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Server.TickWorldGroupsParallel") : default;
        if (worlds.Length < 2) {
            for (int i = 0; i < worlds.Length; i++) {
                TickWorld(worlds[i]);
            }

            return;
        }

        int groupCount = requestedGroupCount ?? (Properties.TickGroups > 0
            ? Properties.TickGroups
            : TickWorkerPool.WorkerCount);
        groupCount = Math.Clamp(groupCount, 1, worlds.Length);
        if (groupCount == 1) {
            for (int i = 0; i < worlds.Length; i++) {
                TickWorld(worlds[i]);
            }

            return;
        }

        List<WorldInstance>[] groups = _worldGroups;
        if (groups.Length < groupCount) {
            int oldLength = groups.Length;
            Array.Resize(ref groups, groupCount);
            for (int i = oldLength; i < groupCount; i++) {
                groups[i] = [];
            }
        }

        for (int i = 0; i < groups.Length; i++) {
            groups[i]?.Clear();
        }
        _worldGroups = groups;

        for (int i = 0; i < worlds.Length; i++) {
            groups[i % groupCount].Add(worlds[i]);
        }

        List<WorldGroupTickTask> tasks = _worldGroupTasks;
        tasks.Clear();
        using CountdownEvent completed = new(groupCount);
        try {
            for (int i = 0; i < groups.Length; i++) {
                WorldGroupTickTask task = new(groups[i], completed);
                if (TickWorkerPool.TryEnqueue(task)) {
                    tasks.Add(task);
                }
                else {
                    for (int worldIndex = 0; worldIndex < groups[i].Count; worldIndex++) {
                        TickWorld(groups[i][worldIndex]);
                    }

                    completed.Signal();
                }
            }

            completed.Wait();
            for (int i = 0; i < tasks.Count; i++) {
                WorldGroupTickTask task = tasks[i];
                if (task.Error is not null) {
                    Logger.Warn($"World group tick failed: {task.Error}");
                }
            }
        }
        finally {
            tasks.Clear();
            for (int i = 0; i < groups.Length; i++) {
                groups[i]?.Clear();
            }
        }
    }

    private int GetAdaptiveGroupCount(WorldInstance[] worlds) {
        if (Properties.TickGroups > 0) {
            LastAdaptiveGroupCount = Math.Clamp(Properties.TickGroups, 1, worlds.Length);
            return LastAdaptiveGroupCount;
        }

        bool hotWorld = worlds.Any(static world => world.TickWork >= AdaptiveHotWorldMilliseconds);
        int groupCount = hotWorld
            ? Math.Min(TickWorkerPool.WorkerCount, worlds.Length)
            : Math.Max(1, (worlds.Length + 1) / 2);
        LastAdaptiveGroupCount = Math.Min(groupCount, TickWorkerPool.WorkerCount);
        return LastAdaptiveGroupCount;
    }

    private readonly record struct PendingPlayerTransfer(
        Dimension Source,
        Dimension Target,
        Vec3 Position);

    public void UpdateTps(long timestamp) {
        if (_lastTpsTimestamp == 0) {
            _lastTpsTimestamp = timestamp;
            _lastTpsTick = GetWorld().TickValue;
            return;
        }

        ulong tickDelta = GetWorld().TickValue - _lastTpsTick;
        if (tickDelta < TpsUpdateIntervalTicks) {
            return;
        }

        long timestampDelta = timestamp - _lastTpsTimestamp;
        if (tickDelta == 0 || timestampDelta <= 0) {
            return;
        }

        double elapsedSeconds = (double)timestampDelta / Stopwatch.Frequency;
        double currentTps = Math.Min(20.0, tickDelta / elapsedSeconds);
        double previousTps = Tps;
        Volatile.Write(ref _tps, previousTps == 0 ? currentTps : previousTps + ((currentTps - previousTps) * 0.2));
        Volatile.Write(ref _tickWorkAverage, _tickWorkSamples == 0 ? 0 : _tickWorkTotal / _tickWorkSamples);
        Volatile.Write(ref _tickWorkMaximumPublished, _tickWorkMaximum);
        _tickWorkTotal = 0;
        _tickWorkMaximum = 0;
        _tickWorkSamples = 0;
        _lastTpsTimestamp = timestamp;
        _lastTpsTick = GetWorld().TickValue;
    }


    public void Broadcast(Packet packet, params PlayerInstance[]? exclude) {
        foreach ((NetworkConnection connection, PlayerInstance player) in Players) {
            if (exclude is not null) {
                bool skipped = false;
                for (int i = 0; i < exclude.Length; i++) {
                    if (ReferenceEquals(exclude[i], player)) {
                        skipped = true;
                        break;
                    }
                }

                if (skipped) {
                    continue;
                }
            }

            Network.QueuePacket(connection, packet);
        }
    }
}







