namespace Basalt.Core.Item.Traits;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;


public static class ItemTraitRegistry
{
    private static readonly Dictionary<string, Type> Traits = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, Type> RegisteredTraits => Traits;

    [RequiresUnreferencedCode("...")]
    public static void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(ItemTrait).IsAssignableFrom(type))
            {
                continue;
            }

            Register(type);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "...")]
    public static void Register(Type traitType)
    {
        if (!typeof(ItemTrait).IsAssignableFrom(traitType))
        {
            throw new ArgumentException($"{traitType.FullName} is not an ItemTrait.", nameof(traitType));
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

        foreach (ItemType itemType in ItemType.GetAll())
        {
            if (Matches(itemType, traitType))
            {
                itemType.RegisterTrait(traitType, identifier);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "...")]
    public static void BindTraitsToType(ItemType itemType)
    {
        foreach ((string identifier, Type traitType) in Traits)
        {
            if (Matches(itemType, traitType))
            {
                itemType.RegisterTrait(traitType, identifier);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "...")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Component types returned via reflection are preserved")]
    private static bool Matches(ItemType itemType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type traitType)
    {
        string[] types = GetStringTargets(traitType, "Types");
        for (int i = 0; i < types.Length; i++)
        {
            if (string.Equals(types[i], itemType.Identifier, StringComparison.Ordinal))
            {
                return true;
            }
        }

        string[] tags = GetStringTargets(traitType, "Tags");
        for (int i = 0; i < tags.Length; i++)
        {
            for (int j = 0; j < itemType.Tags.Count; j++)
            {
                if (string.Equals(tags[i], itemType.Tags[j], StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

#pragma warning disable IL2072
        string[] components = GetComponentTargets(traitType);
#pragma warning restore IL2072
        for (int i = 0; i < components.Length; i++)
        {
            if (itemType.Components.HasComponent(components[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetIdentifier([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type traitType)
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

    private static string[] GetStringTargets([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type traitType, string memberName)
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

    private static string[] GetComponentTargets([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type traitType)
    {
        List<string> identifiers = [];

#pragma warning disable IL2072
        if (traitType.GetField("Component", BindingFlags.Public | BindingFlags.Static) is FieldInfo singleField &&
            singleField.GetValue(null) is Type singleComponentType)
        {
            AddComponentIdentifier(singleComponentType, identifiers);
        }

        if (traitType.GetProperty("Component", BindingFlags.Public | BindingFlags.Static) is PropertyInfo singleProperty &&
            singleProperty.GetValue(null) is Type singlePropertyComponentType)
        {
            AddComponentIdentifier(singlePropertyComponentType, identifiers);
        }

        if (traitType.GetField("Components", BindingFlags.Public | BindingFlags.Static) is FieldInfo field &&
            field.GetValue(null) is IEnumerable<Type> componentTypes)
        {
            foreach (Type componentType in componentTypes)
            {
                AddComponentIdentifier(componentType, identifiers);
            }
        }

        if (traitType.GetProperty("Components", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
            property.GetValue(null) is IEnumerable<Type> propertyComponentTypes)
        {
            foreach (Type componentType in propertyComponentTypes)
            {
                AddComponentIdentifier(componentType, identifiers);
            }
        }
#pragma warning restore IL2072

        return [.. identifiers.Distinct(StringComparer.Ordinal)];
    }

    private static void AddComponentIdentifier([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, List<string> identifiers)
    {
        if (!typeof(Components.ItemTypeComponent).IsAssignableFrom(componentType))
        {
            return;
        }

        if (componentType.GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is not PropertyInfo property ||
            property.PropertyType != typeof(string) ||
            property.GetValue(null) is not string identifier ||
            string.IsNullOrWhiteSpace(identifier))
        {
            return;
        }

        identifiers.Add(identifier);
    }
}






