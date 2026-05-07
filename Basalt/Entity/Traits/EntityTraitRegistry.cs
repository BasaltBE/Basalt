using System.Reflection;
using Basalt.Protocol.Enums;

namespace Basalt.Entity.Traits;

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
        for (int i = 0; i < traitTypes.Length; i++)
        {
            Register(traitTypes[i]);
        }
    }

    public static void Register(Type traitType)
    {
        if (!typeof(EntityTrait).IsAssignableFrom(traitType))
        {
            throw new ArgumentException($"{traitType.FullName} is not an EntityTrait.", nameof(traitType));
        }

        if (traitType.IsAbstract)
        {
            return;
        }

        string identifier = GetIdentifier(traitType);
        if (!Traits.TryAdd(identifier, traitType))
        {
            return;
        }

        string[] types = GetStringTargets(traitType, "Types");
        string[] components = GetStringTargets(traitType, "Components");

        foreach (EntityType entityType in EntityType.GetAll())
        {
            if (Matches(entityType, types, components))
            {
                entityType.RegisterTrait(traitType, identifier);
            }
        }
    }

    public static void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(EntityTrait).IsAssignableFrom(type))
            {
                continue;
            }

            Register(type);
        }
    }

    public static void BindTraitsToType(EntityType entityType)
    {
        foreach ((string identifier, Type traitType) in Traits)
        {
            string[] types = GetStringTargets(traitType, "Types");
            string[] components = GetStringTargets(traitType, "Components");
            if (Matches(entityType, types, components))
            {
                entityType.RegisterTrait(traitType, identifier);
            }
        }
    }

    private static bool Matches(EntityType entityType, string[] types, string[] components)
    {
        for (int i = 0; i < types.Length; i++)
        {
            if (string.Equals(types[i], entityType.Identifier, StringComparison.Ordinal))
            {
                return true;
            }
        }

        for (int i = 0; i < components.Length; i++)
        {
            for (int j = 0; j < entityType.Components.Count; j++)
            {
                if (string.Equals(components[i], entityType.Components[j], StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetIdentifier(Type traitType)
    {
        if (traitType.GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
            property.PropertyType == typeof(string) &&
            property.GetValue(null) is string identifier &&
            !string.IsNullOrWhiteSpace(identifier))
        {
            return identifier;
        }

        return traitType.FullName ?? traitType.Name;
    }

    private static string[] GetStringTargets(Type traitType, string memberName)
    {
        if (traitType.GetField(memberName, BindingFlags.Public | BindingFlags.Static) is FieldInfo field)
        {
            if (field.GetValue(null) is IEnumerable<string> asStrings)
            {
                return [.. asStrings];
            }

            if (field.GetValue(null) is IEnumerable<EntityIdentifier> asEnums)
            {
                string[] values = [.. asEnums.Select(value => value.ToIdentifierString())];
                return values;
            }
        }

        if (traitType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static) is PropertyInfo property)
        {
            if (property.GetValue(null) is IEnumerable<string> asStrings)
            {
                return [.. asStrings];
            }

            if (property.GetValue(null) is IEnumerable<EntityIdentifier> asEnums)
            {
                string[] values = [.. asEnums.Select(value => value.ToIdentifierString())];
                return values;
            }
        }

        return [];
    }
}
