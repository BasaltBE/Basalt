using Basalt.Protocol.Enums;
using Basalt.World.Dimension.Generation;
using Basalt.World.Dimension.Provider;
using DimensionInstance = Basalt.World.Dimension.Dimension;

namespace Basalt.World;

public sealed class World : IDisposable
{
    private readonly Dictionary<string, DimensionInstance> _dimensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _generatorRegistry = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }
    public WorldProvider Provider { get; }
    public ulong CurrentTick { get; private set; }

    public World(string name, WorldProvider? provider = null)
    {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
    }

    public int DimensionCount => _dimensions.Count;

    public IEnumerable<DimensionInstance> Dimensions => _dimensions.Values;

    public void RegisterGenerator<TGenerator>(string identifier) where TGenerator : Generator
    {
        RegisterGenerator(identifier, typeof(TGenerator));
    }

    public void RegisterGenerator(string identifier, Type generatorType)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Generator identifier cannot be empty.", nameof(identifier));
        }

        if (!typeof(Generator).IsAssignableFrom(generatorType))
        {
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));
        }

        _generatorRegistry[identifier] = generatorType;
    }

    public DimensionInstance CreateDimension(string identifier, DimensionType type, string generatorIdentifier = "void", params object[] generatorArgs)
    {
        if (!_generatorRegistry.TryGetValue(generatorIdentifier, out Type? generatorType))
        {
            throw new KeyNotFoundException($"No generator registered with identifier '{generatorIdentifier}'.");
        }

        object? instance = Activator.CreateInstance(generatorType, generatorArgs);
        if (instance is not Generator generator)
        {
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");
        }

        DimensionInstance dimension = new(identifier, type, Provider, generator);
        AddDimension(dimension);
        return dimension;
    }

    public DimensionInstance CreateDimension(string identifier, DimensionType type, Type generatorType, params object[] generatorArgs)
    {
        if (!typeof(Generator).IsAssignableFrom(generatorType))
        {
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));
        }

        object? instance = Activator.CreateInstance(generatorType, generatorArgs);
        if (instance is not Generator generator)
        {
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");
        }

        DimensionInstance dimension = new(identifier, type, Provider, generator);
        AddDimension(dimension);
        return dimension;
    }

    public void AddDimension(DimensionInstance dimension)
    {
        dimension.World = this;
        _dimensions[dimension.Identifier] = dimension;
    }

    public bool RemoveDimension(string identifier)
    {
        if (!_dimensions.Remove(identifier, out DimensionInstance? dimension))
        {
            return false;
        }

        dimension.Dispose();
        return true;
    }

    public DimensionInstance? GetDimension(string identifier)
    {
        _dimensions.TryGetValue(identifier, out DimensionInstance? dimension);
        return dimension;
    }

    public DimensionInstance? GetDimension(DimensionType type)
    {
        foreach (DimensionInstance dimension in _dimensions.Values)
        {
            if (dimension.Type == type)
            {
                return dimension;
            }
        }

        return null;
    }

    public void Dispose()
    {
        foreach (DimensionInstance dimension in _dimensions.Values)
        {
            dimension.Dispose();
        }

        _dimensions.Clear();
        Provider.Dispose();
    }

    public void Tick()
    {
        CurrentTick++;
        foreach (DimensionInstance dimension in _dimensions.Values)
        {
            dimension.Tick(CurrentTick, 1);
        }
    }
}
