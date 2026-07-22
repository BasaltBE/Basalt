namespace Basalt.Core.Entities;

using Basalt.Core.Entities.Traits;
using System.Text.Json;

public sealed class EntityType {
    private static readonly Dictionary<string, EntityType> Registry = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Type> _traits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _componentProperties;
    private readonly Dictionary<string, Dictionary<string, JsonElement>> _componentGroupProperties;

    public string Identifier { get; }
    public IReadOnlyList<string> Components { get; }
    public string? LootTablePath { get; }
    public IReadOnlyDictionary<string, JsonElement> ComponentProperties => _componentProperties;
    public IReadOnlyDictionary<string, Dictionary<string, JsonElement>> ComponentGroupProperties => _componentGroupProperties;
    public IReadOnlyDictionary<string, Type> Traits => _traits;
    public static IReadOnlyDictionary<string, EntityType> Types => Registry;

    public EntityType(string identifier, IEnumerable<string>? components, EntityPropertiesPayloadData? propertiesPayload = null, string? lootTablePath = null) {
        Identifier = identifier;
        Components = components is null ? [] : [.. components];
        LootTablePath = string.IsNullOrWhiteSpace(lootTablePath) ? null : lootTablePath;
        _componentProperties = propertiesPayload?.Components is null
            ? []
            : new Dictionary<string, JsonElement>(propertiesPayload.Components, StringComparer.Ordinal);
        _componentGroupProperties = propertiesPayload?.ComponentGroups is null
            ? []
            : new Dictionary<string, Dictionary<string, JsonElement>>(propertiesPayload.ComponentGroups, StringComparer.Ordinal);
        Registry[identifier] = this;
        EntityTraitRegistry.BindTraitsToType(this);
    }

    public static EntityType? Get(string identifier) {
        return Registry.TryGetValue(identifier, out EntityType? type) ? type : null;
    }

    public static EntityType GetOrCreate(string identifier) {
        return Get(identifier) ?? new EntityType(identifier, []);
    }

    public static List<EntityType> GetAll() {
        return [.. Registry.Values];
    }

    public static void EnsureRegistryCapacity(int capacity) {
        Registry.EnsureCapacity(capacity);
    }

    public void RegisterTrait(Type traitType, string identifier) {
        _traits.TryAdd(identifier, traitType);
    }

    public bool TryGetComponentProperties(string component, out JsonElement properties) {
        return _componentProperties.TryGetValue(component, out properties);
    }
}






