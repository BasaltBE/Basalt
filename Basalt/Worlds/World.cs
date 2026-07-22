namespace Basalt.Core.Worlds;

using System.Diagnostics.CodeAnalysis;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Dimension = Dimensions.Dimension;

public sealed class World : IDisposable, Tickable {
    private readonly Dictionary<string, Dimension> _dimensions = new(StringComparer.OrdinalIgnoreCase);


    /// <summary>
    /// The name of the world.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The world provider, used for storing and loading dimensions.
    /// </summary>
    public WorldProvider Provider { get; }

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
    public Dimension CreateDimension(string identifier, DimensionType type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type generatorType, params object[] generatorArgs) {
        return CreateDimension(identifier, type, new Vec3f(0, 80, 0), generatorType, generatorArgs);
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
    public Dimension CreateDimension(string identifier, DimensionType type, Vec3f spawnPosition, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type generatorType, params object[] generatorArgs) {
        if (!typeof(Generator).IsAssignableFrom(generatorType))
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));

        if (Activator.CreateInstance(generatorType, generatorArgs) is not Generator generator)
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");

        Dimension dimension = new(identifier, type, Provider, generator);
        dimension.SpawnPosition = spawnPosition;

        Vec3f? stored = Provider.LoadSpawnPosition(type);
        if (stored.HasValue) {
            dimension.SpawnPosition = stored.Value;
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
    public Dimension? GetDimension(DimensionType type) =>
        _dimensions.Values.FirstOrDefault(d => d.Type == type);

    /// <summary>
    /// Ticks the world and all its dimensions.
    /// Please dont tick manually unless you know what you are doing, we aint gonna be at fault if u do.
    /// </summary>
    public void Tick() {
        TickValue++;
        Scheduler?.Tick();
        foreach (Dimension dimension in _dimensions.Values) {
            using var _ = Profiler.BeginZone($"Dimension.Tick({dimension.Identifier})");
            dimension.Tick(TickValue, 1);
        }
    }

    /// <summary>
    /// Saves all dirty chunks across all dimensions.
    /// </summary>
    public void Save() {
        foreach (Dimension dimension in _dimensions.Values) {
            dimension.SaveDirtyChunks();
        }
    }

    /// <summary>
    /// Disposes of the world and its dimensions.
    /// </summary>
    public void Dispose() {
        Scheduler?.Stop();

        foreach (Dimension dimension in _dimensions.Values)
            dimension.Dispose();

        _dimensions.Clear();
        Provider.Dispose();
    }
}






