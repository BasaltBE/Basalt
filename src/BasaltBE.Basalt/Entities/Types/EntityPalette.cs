using System.Reflection;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq.Expressions;
using BasaltBE.Basalt.Entities.Traits;

namespace BasaltBE.Basalt.Entities.Types;

public static class EntityPalette
{
    public static EntityType[] Types = LoadTypes();
    static readonly Dictionary<string, EntityType> ByIdentifier = Types.ToDictionary(type => type.Identifier);
    static readonly Dictionary<int, EntityType> ByRuntimeId = Types
        .Where(type => type.RuntimeId.HasValue)
        .ToDictionary(type => type.RuntimeId!.Value);
    static readonly List<TraitRegistration> TraitRegistrations = [];
    static readonly ConcurrentDictionary<EntityType, EntityTraitFactory[]> TraitFactories = new(ReferenceEqualityComparer.Instance);
    static readonly Dictionary<string, CustomEntity> Definitions = new(StringComparer.Ordinal);
    internal static bool Frozen;

    internal static CustomEntity Create(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        string[] parts = identifier.Split(':');
        if (parts.Length != 2 || parts.Any(part => part.Length == 0 || part.Any(character =>
            !char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character is not ('_' or '-' or '.' or '/'))) ||
            parts[0].Contains('/'))
            throw new ArgumentException("Use a namespaced entity identifier.", nameof(identifier));

        if (Frozen)
            throw new InvalidOperationException("Configure entities before starting the server.");

        if (Definitions.TryGetValue(identifier, out CustomEntity? definition))
            return definition;

        EntityType? type = Find(identifier);
        if (type is null)
        {
            type = new EntityType { Identifier = identifier, Summonable = true, PropertiesPayload = new() };
            ByIdentifier.Add(identifier, type);
            Types = [.. Types, type];
        }

        definition = new CustomEntity(type);
        Definitions.Add(identifier, definition);
        return definition;
    }

    internal static void Freeze()
    {
        foreach (EntityType type in Types)
            CacheTraitFactories(type);
        Frozen = true;
    }

    public static EntityType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    internal static void Invalidate(EntityType type) => TraitFactories.TryRemove(type, out _);

    public static EntityType? FindByRuntimeId(int runtimeId) => ByRuntimeId.GetValueOrDefault(runtimeId);

    public static void RegisterTrait<TTrait>(string? identifier = null) where TTrait : EntityTrait
    {
        if (Frozen)
            throw new InvalidOperationException("Register entity traits before starting the server.");
        Type traitType = typeof(TTrait);
        EntityTraitAppliesToAttribute targets = identifier is not null
            ? new EntityTraitAppliesToAttribute { Identifiers = [identifier] }
            : traitType.GetCustomAttribute<EntityTraitAppliesToAttribute>()
            ?? throw new InvalidOperationException($"Trait '{traitType}' has no EntityTraitAppliesTo attribute.");
        if (TraitRegistrations.Any(registration => registration.TraitType == traitType &&
            (identifier is null || registration.Matches(Find(identifier)
                ?? throw new KeyNotFoundException($"Entity '{identifier}' was not found.")))))
            throw new InvalidOperationException($"Trait '{traitType}' is already registered for this entity.");
        if (targets.Identifiers.Length + targets.Families.Length + targets.Components.Length == 0)
            throw new InvalidOperationException($"Trait '{traitType}' has no entity targets.");

        ConstructorInfo constructor = traitType.GetConstructor([typeof(Entity)])
            ?? throw new InvalidOperationException($"Trait '{traitType}' must have a public constructor that takes an Entity.");
        ParameterExpression entity = Expression.Parameter(typeof(Entity));
        EntityTraitFactory factory = Expression.Lambda<EntityTraitFactory>(
            Expression.Convert(Expression.New(constructor, entity), typeof(EntityTrait)),
            entity
        ).Compile();
        TraitRegistration registration = new(traitType, targets, factory);
        TraitRegistrations.Add(registration);

        foreach (EntityType type in Types)
            CacheTraitFactories(type);
    }

    internal static EntityTrait[]? CreateTraits(Entity entity, EntityType? type)
    {
        if (type is null)
            return null;

        EntityTraitFactory[] factories = TraitFactories.GetValueOrDefault(type) ?? CacheTraitFactories(type);
        if (factories.Length == 0)
            return null;

        EntityTrait[] traits = new EntityTrait[factories.Length];
        for (int i = 0; i < factories.Length; i++)
        {
            traits[i] = factories[i](entity);
        }

        return traits;
    }

    static EntityTraitFactory[] CacheTraitFactories(EntityType type)
    {
        List<EntityTraitFactory> factories = [];
        HashSet<Type> included = [];
        foreach (TraitRegistration registration in TraitRegistrations)
        {
            if (registration.Matches(type) && included.Add(registration.TraitType))
                factories.Add(registration.Factory);
        }

        EntityTraitFactory[] result = factories.ToArray();
        TraitFactories[type] = result;
        return result;
    }

    static EntityType[] LoadTypes()
    {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.entity-types.json")
            ?? throw new InvalidOperationException("Entity data file 'entity-types.json' is missing.");

        return JsonSerializer.Deserialize<EntityType[]>(stream)
            ?? throw new JsonException("Entity data file 'entity-types.json' is empty.");
    }

    delegate EntityTrait EntityTraitFactory(Entity entity);

    sealed class TraitRegistration(Type traitType, EntityTraitAppliesToAttribute targets, EntityTraitFactory factory)
    {
        public readonly Type TraitType = traitType;
        public readonly EntityTraitFactory Factory = factory;

        public bool Matches(EntityType type) =>
            targets.Identifiers.Contains(type.Identifier, StringComparer.Ordinal)
            || targets.Families.Intersect(type.Families, StringComparer.Ordinal).Any()
            || targets.Components.Intersect(type.Components, StringComparer.Ordinal).Any();
    }
}
