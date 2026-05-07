namespace Basalt.Entity;

public sealed class EntityType
{
    private static readonly Dictionary<string, EntityType> Registry = new(StringComparer.Ordinal);

    public string Identifier { get; }
    public IReadOnlyList<string> Components { get; }
    public static IReadOnlyDictionary<string, EntityType> Types => Registry;

    public EntityType(string identifier, IEnumerable<string>? components)
    {
        Identifier = identifier;
        Components = components is null ? [] : [.. components];
        Registry[identifier] = this;
    }

    public static EntityType? Get(string identifier)
    {
        return Registry.TryGetValue(identifier, out EntityType? type) ? type : null;
    }

    public static EntityType GetOrPlayer(string identifier)
    {
        return Get(identifier) ?? Get("minecraft:player") ?? new EntityType("minecraft:player", []);
    }

    public static List<EntityType> GetAll()
    {
        return [.. Registry.Values];
    }

    public static void EnsureRegistryCapacity(int capacity)
    {
        Registry.EnsureCapacity(capacity);
    }
}
