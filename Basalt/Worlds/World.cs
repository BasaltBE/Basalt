namespace Basalt.Core.Worlds;

using System.Diagnostics.CodeAnalysis;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Dimension = Dimensions.Dimension;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Types;

public sealed class World : IDisposable, Tickable {
    private readonly Dictionary<string, Dimension> _dimensions = new(StringComparer.OrdinalIgnoreCase);
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
                Scheduler = new WorldScheduler(this, value.WorkerPool);
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

    /// <summary>
    /// The amount of milliseconds the last tick took.
    /// </summary>
    public double TickWork { get; set; }

    /// <summary>
    /// The amount of dimensions in the world.
    /// </summary>
    public int DimensionCount => _dimensions.Count;

    /// <summary>
    /// An enumerable of all dimensions in the world.
    /// </summary>
    public IEnumerable<Dimension> Dimensions => _dimensions.Values;

    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public World(string name, WorldProvider? provider = null) {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
        Persistence = new WorldPersistence(Provider);
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
        if (stored is not null) {
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
        dimension.World = this;
        _dimensions[dimension.Identifier] = dimension;
    }


    /// <summary>
    /// Removes a dimension from the world.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public bool RemoveDimension(string identifier) {
        if (!_dimensions.Remove(identifier, out Dimension? dimension))
            return false;

        dimension.Dispose();
        return true;
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
    public void Tick() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("World.Tick") : default;
        TickValue++;
        Scheduler?.Tick();
        foreach (Dimension dimension in _dimensions.Values) {
            using var _ = Profiler.Enabled ? Profiler.BeginZone($"Dimension.Tick({dimension.Identifier})") : default;
            dimension.Tick(TickValue, 1);
        }
    }

    /// <summary>
    /// Saves all dirty chunks across all dimensions and writes level.dat.
    /// </summary>
    public void Save() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("World.Save") : default;
        _autoSaveDimensions = null;
        Persistence.Flush();
        foreach (Dimension dimension in _dimensions.Values) {
            dimension.SaveDirtyChunks();
        }

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






