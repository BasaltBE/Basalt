using Basalt.Protocol.Enums;
using Basalt.World.Dimension.Provider;
using DimensionInstance = Basalt.World.Dimension.Dimension;

namespace Basalt.World;

public sealed class World : IDisposable
{
    private readonly Dictionary<string, DimensionInstance> _dimensions = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }
    public WorldProvider Provider { get; }

    public World(string name, WorldProvider? provider = null)
    {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
    }

    public int DimensionCount => _dimensions.Count;

    public IEnumerable<DimensionInstance> Dimensions => _dimensions.Values;

    public void AddDimension(DimensionInstance dimension)
    {
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
}
