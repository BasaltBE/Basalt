namespace Basalt.Core.Entities.Traits;

using System.Reflection;
using Basalt.Protocol.Enums;


public static class EntityTraitRegistry
{
    private static readonly Dictionary<string, Type> Traits = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, Type> RegisteredTraits => Traits;

    public static void Register<TTrait>() where TTrait : EntityTrait
    {
        Register(typeof(TTrait));
    }

    public static void Register(params Type[] traitTypes)
    {
        foreach (Type traitType in traitTypes)
        {
            Register(traitType);
        }
    }

    public static void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            Register(type);
        }
    }

    public static void Register(Type traitType)
    {
        if (traitType.IsAbstract || !typeof(EntityTrait).IsAssignableFrom(traitType))
        {
            return;
        }

        string identifier = GetIdentifier(traitType);

        if (!Traits.TryAdd(identifier, traitType))
        {
            return;
        }

        foreach (EntityType entityType in EntityType.GetAll())
        {
            if (TraitAppliesTo(entityType, traitType))
            {
                entityType.RegisterTrait(traitType, identifier);
            }
        }
    }

    public static void BindTraitsToType(EntityType entityType)
    {
        foreach ((string identifier, Type traitType) in Traits)
        {
            if (TraitAppliesTo(entityType, traitType))
            {
                entityType.RegisterTrait(traitType, identifier);
            }
        }
    }

    private static bool TraitAppliesTo(EntityType entityType, Type traitType)
    {
        foreach (string targetType in ReadTraitTargets(traitType, "Types"))
        {
            if (targetType == entityType.Identifier)
            {
                return true;
            }
        }

        foreach (string component in ReadTraitTargets(traitType, "Components"))
        {
            if (entityType.Components.Contains(component))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetIdentifier(Type traitType)
    {
        PropertyInfo? property = traitType.GetProperty(
            "Identifier",
            BindingFlags.Public | BindingFlags.Static);

        if (property is not null &&
            property.PropertyType == typeof(string) &&
            property.GetValue(null) is string identifier &&
            !string.IsNullOrWhiteSpace(identifier))
        {
            return identifier;
        }

        return traitType.FullName ?? traitType.Name;
    }

    private static string[] ReadTraitTargets(Type traitType, string name)
    {
        object? value = null;

        FieldInfo? field = traitType.GetField(name, BindingFlags.Public | BindingFlags.Static);
        if (field is not null)
        {
            value = field.GetValue(null);
        }

        PropertyInfo? property = traitType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (value is null && property is not null)
        {
            value = property.GetValue(null);
        }

        if (value is IEnumerable<string> strings)
        {
            return [.. strings];
        }

        if (value is IEnumerable<EntityIdentifier> identifiers)
        {
            List<string> values = [];

            foreach (EntityIdentifier identifier in identifiers)
            {
                values.Add(identifier.ToIdentifierString());
            }

            return [.. values];
        }

        return [];
    }
}





