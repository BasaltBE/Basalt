using System.Reflection;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq.Expressions;
using BasaltBE.Basalt.Entities.Traits;

namespace BasaltBE.Basalt.Entities.Types;

public static class EntityPalette {
    public static readonly EntityType[] Types = LoadTypes();
    static readonly Dictionary<string, EntityType> ByIdentifier = Types.ToDictionary(type => type.Identifier);
    static readonly Dictionary<int, EntityType> ByRuntimeId = Types
        .Where(type => type.RuntimeId.HasValue)
        .ToDictionary(type => type.RuntimeId!.Value);
    static readonly List<TraitRegistration> TraitRegistrations = [];
    static readonly ConcurrentDictionary<EntityType, EntityTraitFactory[]> TraitFactories = new(ReferenceEqualityComparer.Instance);

    public static EntityType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static EntityType? FindByRuntimeId(int runtimeId) => ByRuntimeId.GetValueOrDefault(runtimeId);

    public static void RegisterTrait<TTrait>() where TTrait : EntityTrait {
        Type traitType = typeof(TTrait);
        if (TraitRegistrations.Any(registration => registration.TraitType == traitType))
            throw new InvalidOperationException($"Trait '{traitType}' is already registered.");

        EntityTraitAppliesToAttribute targets = traitType.GetCustomAttribute<EntityTraitAppliesToAttribute>()
            ?? throw new InvalidOperationException($"Trait '{traitType}' has no EntityTraitAppliesTo attribute.");
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

    internal static EntityTrait[]? CreateTraits(Entity entity, EntityType? type) {
        if (type is null)
            return null;

        EntityTraitFactory[] factories = TraitFactories.GetValueOrDefault(type) ?? CacheTraitFactories(type);
        if (factories.Length == 0)
            return null;

        EntityTrait[] traits = new EntityTrait[factories.Length];
        for (int i = 0; i < factories.Length; i++) {
            traits[i] = factories[i](entity);
        }

        return traits;
    }

    static EntityTraitFactory[] CacheTraitFactories(EntityType type) {
        List<EntityTraitFactory> factories = [];
        foreach (TraitRegistration registration in TraitRegistrations) {
            if (registration.Matches(type))
                factories.Add(registration.Factory);
        }

        EntityTraitFactory[] result = factories.ToArray();
        TraitFactories[type] = result;
        return result;
    }

    static EntityType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.entity-types.json")
            ?? throw new InvalidOperationException("Entity data file 'entity-types.json' is missing.");

        return JsonSerializer.Deserialize<EntityType[]>(stream)
            ?? throw new JsonException("Entity data file 'entity-types.json' is empty.");
    }

    delegate EntityTrait EntityTraitFactory(Entity entity);

    sealed class TraitRegistration(Type traitType, EntityTraitAppliesToAttribute targets, EntityTraitFactory factory) {
        public readonly Type TraitType = traitType;
        public readonly EntityTraitFactory Factory = factory;

        public bool Matches(EntityType type) =>
            targets.Identifiers.Contains(type.Identifier, StringComparer.Ordinal)
            || targets.Families.Intersect(type.Families, StringComparer.Ordinal).Any()
            || targets.Components.Intersect(type.Components, StringComparer.Ordinal).Any();
    }
}
