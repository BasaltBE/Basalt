namespace Basalt.Core.Worlds;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Dimension = Dimensions.Dimension;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Player;
using Basalt.Core.Enums;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;

public sealed class World : IDisposable, Tickable {
    public const long DayLength = 24000;
    public const int MaxY = 319;
    private readonly Dictionary<string, Dimension> _dimensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _pendingDimensionRemovals = new();
    private Player[] _playersSnapshot = [];
    private int _playersSnapshotDirty = 1;
    private double _tickWork;
    private Dimension[]? _autoSaveDimensions;
    private int _autoSaveDimensionIndex;

    /// <summary>
    /// The name of the world.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The world provider, used for storing and loading dimensions.
    /// </summary>
    public WorldProvider Provider { get; }
    internal WorldPersistence Persistence { get; }

    /// <summary>
    /// The Server instance.
    /// </summary>
    public Server? Server {
        get => _server;
        internal set {
            _server = value;
            if (value is not null && Scheduler is null)
                Scheduler = new WorldScheduler(
                    this,
                    value.BackgroundWorkerPool,
                    () => value.Plugins.CurrentRegistrationPlugin,
                    value.Plugins.ConfigureTask);
            if (value is not null)
                PublishPlayersSnapshot();
        }
    }

    private Server? _server;

    /// <summary>
    /// The per-world task scheduler.
    /// </summary>
    public WorldScheduler? Scheduler { get; private set; }

    /// <summary>
    /// The current tick value.
    /// </summary>
    public ulong TickValue { get; set; }

    public long DayTime { get; private set; }

    public int CurrentDayTime => (int)(DayTime % DayLength);

    /// <summary>
    /// The amount of milliseconds the last tick took.
    /// </summary>
    public double TickWork {
        get => Volatile.Read(ref _tickWork);
        set => Volatile.Write(ref _tickWork, value);
    }

    /// <summary>
    /// The amount of dimensions in the world.
    /// </summary>
    public int DimensionCount => _dimensions.Count;

    /// <summary>
    /// An enumerable of all dimensions in the world.
    /// </summary>
    public IEnumerable<Dimension> Dimensions => _dimensions.Values;

    public IEnumerable<Player> GetPlayers() {
        foreach (Dimension dimension in _dimensions.Values) {
            foreach (Player player in dimension.GetPlayers()) {
                yield return player;
            }
        }
    }

    public Player[] GetPlayersSnapshot() => Server is null
        ? [.. _dimensions.Values.SelectMany(static dimension => dimension.GetPlayersSnapshot())]
        : [.. Volatile.Read(ref _playersSnapshot)];

    internal void MarkPlayersSnapshotDirty() {
        Volatile.Write(ref _playersSnapshotDirty, 1);
    }

    private void PublishPlayersSnapshot() {
        if (Interlocked.Exchange(ref _playersSnapshotDirty, 0) == 0)
            return;

        Volatile.Write(ref _playersSnapshot, [.. _dimensions.Values.SelectMany(static dimension => dimension.GetPlayersSnapshot())]);
    }

    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public World(string name, WorldProvider? provider = null) {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
        Persistence = new WorldPersistence(Provider);
        (DayTime, TickValue) = Provider.LoadWorldTime();
    }

    public static void ConfigurePersistence(string dataPath) {
    }

    /// <summary>
    /// Creates a new dimension and adds it to the world.
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="type"></param>
    /// <param name="generatorType"></param>
    /// <param name="generatorArgs"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public Dimension CreateDimension(string identifier, DimensionId type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type generatorType, params object[] generatorArgs) {
        return CreateDimension(identifier, type, new Vec3() { X = 0, Y = 80, Z = 0 }, generatorType, generatorArgs);
    }

    /// <summary>
    /// Creates a new dimension with a spawn position and adds it to the world.
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="type"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="generatorType"></param>
    /// <param name="generatorArgs"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public Dimension CreateDimension(string identifier, DimensionId type, Vec3 spawnPosition, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type generatorType, params object[] generatorArgs) {
        if (!typeof(Generator).IsAssignableFrom(generatorType))
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));

        if (Activator.CreateInstance(generatorType, generatorArgs) is not Generator generator)
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");

        Dimension dimension = new(identifier, type, Provider, generator);
        dimension.SpawnPosition = spawnPosition;

        Vec3? stored = Provider.LoadSpawnPosition(type);
        if (stored is { Y: >= 0 }) {
            dimension.SpawnPosition = stored;
        }

        AddDimension(dimension);
        return dimension;
    }

    /// <summary>
    /// Adds a dimension to the world.
    /// </summary>
    /// <param name="dimension"></param>
    public void AddDimension(Dimension dimension) {
        if (Server is { } server) {
            dimension.RegionChunkSize = Math.Clamp(server.Properties.RegionChunkSize, 1, 64);
        }

        dimension.World = this;
        _dimensions[dimension.Identifier] = dimension;
        MarkPlayersSnapshotDirty();
        PublishPlayersSnapshot();
    }


    /// <summary>
    /// Removes a dimension from the world.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public bool RemoveDimension(string identifier) {
        if (!_dimensions.ContainsKey(identifier))
            return false;

        if (Volatile.Read(ref _ticking) != 0) {
            _pendingDimensionRemovals.Enqueue(identifier);
            return true;
        }

        RemoveDimensionNow(identifier);
        return true;
    }

    private void RemoveDimensionNow(string identifier) {
        if (!_dimensions.Remove(identifier, out Dimension? dimension)) {
            return;
        }

        dimension.Dispose();
        MarkPlayersSnapshotDirty();
        PublishPlayersSnapshot();
    }

    /// <summary>
    /// Gets a dimension by its identifier.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public Dimension? GetDimension(string identifier) =>
        _dimensions.TryGetValue(identifier, out Dimension? dimension) ? dimension : null;

    /// <summary>
    /// Gets a dimension by its type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Dimension? GetDimension(DimensionId type) =>
        _dimensions.Values.FirstOrDefault(d => d.Type == type);

    /// <summary>
    /// Ticks the world and all its dimensions.
    /// Please dont tick manually unless you know what you are doing, we aint gonna be at fault if u do.
    /// </summary>
    private int _ticking;

    public void Tick() {
        if (Interlocked.Exchange(ref _ticking, 1) != 0) {
            return;
        }

        try {
            TickCore();
        }
        finally {
            Volatile.Write(ref _ticking, 0);
            while (_pendingDimensionRemovals.TryDequeue(out string? identifier)) {
                RemoveDimensionNow(identifier);
            }
        }
    }

    private void TickCore() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("World.Tick") : default;
        TickValue++;
        if (GetDimension(DimensionId.Overworld)?.Gamerules.DaylightCycle != false) {
            DayTime++;
        }

        if (TickValue % 100 == 0) {
            SendTime();
        }

        Scheduler?.Tick();
        bool parallelDimensions = Server?.Properties.TickMode is TickMode.Dimension or TickMode.Adaptive &&
            _dimensions.Count > 1 &&
            Provider.SupportsConcurrentDimensions;
        if (parallelDimensions) {
            TickDimensionsParallel();
        }
        else {
            foreach (Dimension dimension in _dimensions.Values) {
                using var _ = Profiler.Enabled ? Profiler.BeginZone($"Dimension.Tick({dimension.Identifier})") : default;
                dimension.Tick(TickValue, 1);
            }
        }

        PublishPlayersSnapshot();
    }

    internal void TickDimensionsParallel() {
        if (Server?.TickWorkerPool is not { } workerPool) {
            foreach (Dimension dimension in _dimensions.Values) {
                dimension.Tick(TickValue, 1);
            }
            return;
        }

        TickDimensionsParallel(workerPool);
    }

    internal void TickDimensionsParallel(TaskWorkerPool workerPool) {
        ArgumentNullException.ThrowIfNull(workerPool);

        List<(DimensionTickTask Task, ManualResetEventSlim Completed)> tasks = [];
        foreach (Dimension dimension in _dimensions.Values) {
            ManualResetEventSlim completed = new();
            DimensionTickTask task = new(dimension, TickValue, 1, completed);
            if (workerPool.TryEnqueue(task)) {
                tasks.Add((task, completed));
            }
            else {
                completed.Dispose();
                dimension.Tick(TickValue, 1);
            }
        }

        for (int i = 0; i < tasks.Count; i++) {
            (DimensionTickTask task, ManualResetEventSlim completed) = tasks[i];
            completed.Wait();
            completed.Dispose();
            if (task.Error is not null) {
                Logger.Warn($"Dimension tick failed: {task.Error}");
            }
        }
    }

    public void SetDayTime(long time) {
        DayTime = time;
        SendTime();
    }

    private void SendTime() {
        int time = unchecked((int)DayTime);
        foreach (Dimension dimension in _dimensions.Values) {
            dimension.Broadcast(new SetTimePacket { Time = time });
        }
    }

    /// <summary>
    /// Saves all dirty chunks across all dimensions and writes level.dat.
    /// </summary>
    public void Save() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("World.Save") : default;
        _autoSaveDimensions = null;
        foreach (Dimension dimension in _dimensions.Values) {
            dimension.SaveDirtyChunks();
        }

        Persistence.Flush();
        Provider.WriteLevelDat(this);
    }

    internal void BeginAutoSave() {
        _autoSaveDimensions = [.. _dimensions.Values];
        _autoSaveDimensionIndex = 0;
        for (int i = 0; i < _autoSaveDimensions.Length; i++) {
            _autoSaveDimensions[i].BeginAutoSave();
        }
    }

    internal int AutoSave(int limit) {
        if (_autoSaveDimensions is null || limit <= 0) {
            return 0;
        }

        int saved = 0;
        while (saved < limit && _autoSaveDimensionIndex < _autoSaveDimensions.Length) {
            Dimension dimension = _autoSaveDimensions[_autoSaveDimensionIndex];
            saved += dimension.AutoSave(limit - saved);
            if (dimension.AutoSaving) {
                break;
            }
            _autoSaveDimensionIndex++;
        }

        if (_autoSaveDimensionIndex >= _autoSaveDimensions.Length) {
            Provider.WriteLevelDat(this);
            _autoSaveDimensions = null;
        }

        return saved;
    }

    internal bool AutoSaving => _autoSaveDimensions is not null;

    /// <summary>
    /// Disposes of the world and its dimensions.
    /// </summary>
    public void Dispose() {
        Scheduler?.Stop();
        _autoSaveDimensions = null;

        foreach (Dimension dimension in _dimensions.Values)
            dimension.Dispose();

        _dimensions.Clear();
        Persistence.Dispose();
        Provider.Dispose();
    }
}






