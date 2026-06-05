namespace Basalt.Core.World;

using Basalt.Protocol.Enums;
using Basalt.Core.World.Dimension.Generation;
using Basalt.Core.World.Dimension.Provider;
using DimensionInstance = Basalt.Core.World.Dimension.Dimension;

public sealed class World : IDisposable, Tickable
{
    private readonly Dictionary<string, DimensionInstance> _dimensions = new(StringComparer.OrdinalIgnoreCase);


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
    public global::Basalt.Core.Server? Server { get; internal set; }

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
    public IEnumerable<DimensionInstance> Dimensions => _dimensions.Values;

    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public World(string name, WorldProvider? provider = null)
    {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
    }

    public void ConfigurePersistence(string dataPath)
    {
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
    public DimensionInstance CreateDimension(string identifier, DimensionType type, Type generatorType, params object[] generatorArgs)
    {
        if (!typeof(Generator).IsAssignableFrom(generatorType))
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));

        if (Activator.CreateInstance(generatorType, generatorArgs) is not Generator generator)
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");

        DimensionInstance dimension = new(identifier, type, Provider, generator);
        AddDimension(dimension);
        return dimension;
    }

    /// <summary>
    /// Adds a dimension to the world.
    /// </summary>
    /// <param name="dimension"></param>
    public void AddDimension(DimensionInstance dimension)
    {
        dimension.World = this;
        _dimensions[dimension.Identifier] = dimension;
    }


    /// <summary>
    /// Removes a dimension from the world.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public bool RemoveDimension(string identifier)
    {
        if (!_dimensions.Remove(identifier, out DimensionInstance? dimension))
            return false;

        dimension.Dispose();
        return true;
    }

    /// <summary>
    /// Gets a dimension by its identifier.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public DimensionInstance? GetDimension(string identifier) =>
        _dimensions.TryGetValue(identifier, out DimensionInstance? dimension) ? dimension : null;

    /// <summary>
    /// Gets a dimension by its type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public DimensionInstance? GetDimension(DimensionType type) =>
        _dimensions.Values.FirstOrDefault(d => d.Type == type);

    /// <summary>
    /// Ticks the world and all its dimensions.
    /// Please dont tick manually unless you know what you are doing, we aint gonna be at fault if u do.
    /// </summary>
    public void Tick()
    {
        TickValue++;
        foreach (DimensionInstance dimension in _dimensions.Values)
            dimension.Tick(TickValue, 1);
    }

    /// <summary>
    /// Disposes of the world and its dimensions.
    /// </summary>
    public void Dispose()
    {
        foreach (DimensionInstance dimension in _dimensions.Values)
            dimension.Dispose();

        _dimensions.Clear();
        Provider.Dispose();
    }
}






