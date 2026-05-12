using System.Reflection;

namespace Basalt.Block.Traits;

public static class BlockTraitRegistry
{
    private static readonly Dictionary<string, Type> Traits = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, Type> RegisteredTraits => Traits;

    public static void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(BlockTrait).IsAssignableFrom(type))
            {
                continue;
            }

            Register(type);
        }
    }

    public static void Register(Type traitType)
    {
        if (!typeof(BlockTrait).IsAssignableFrom(traitType))
        {
            throw new ArgumentException($"{traitType.FullName} is not a BlockTrait.", nameof(traitType));
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

        foreach (BlockType blockType in BlockType.Types.Values)
        {
            if (Matches(blockType, traitType))
            {
                blockType.RegisterTrait(traitType, identifier);
            }
        }
    }

    public static void BindTraitsToType(BlockType blockType)
    {
        foreach ((string identifier, Type traitType) in Traits)
        {
            if (Matches(blockType, traitType))
            {
                blockType.RegisterTrait(traitType, identifier);
            }
        }
    }

    private static bool Matches(BlockType blockType, Type traitType)
    {
        string[] types = GetStringTargets(traitType, "Types");
        for (int i = 0; i < types.Length; i++)
        {
            if (string.Equals(types[i], blockType.Identifier, StringComparison.Ordinal))
            {
                return true;
            }
        }

        // Add Tags or Components matching later if BlockType gets them.

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
        if (traitType.GetField(memberName, BindingFlags.Public | BindingFlags.Static) is FieldInfo field &&
            field.GetValue(null) is IEnumerable<string> values)
        {
            return [.. values];
        }

        if (traitType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
            property.GetValue(null) is IEnumerable<string> propertyValues)
        {
            return [.. propertyValues];
        }

        return [];
    }
}
