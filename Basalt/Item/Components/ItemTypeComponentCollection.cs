namespace Basalt.Server.Item.Components;

using Basalt.Protocol.Nbt;
using System.Reflection;


public sealed class ItemTypeComponentCollection
{
    private readonly Dictionary<string, CompoundTag> _components;
    private readonly ItemType _itemType;

    public ItemTypeComponentCollection(ItemType itemType, CompoundTag properties)
    {
        _itemType = itemType;
        _components = new Dictionary<string, CompoundTag>(StringComparer.Ordinal);
        if (properties.Get<CompoundTag>("components") is CompoundTag componentsTag)
        {
            foreach ((string key, BaseTag value) in componentsTag.Values)
            {
                if (value is CompoundTag compound)
                {
                    _components[key] = compound;
                }
            }
        }

        if (properties.Get<ListTag>("components") is not ListTag componentsList)
        {
            return;
        }

        for (int i = 0; i < componentsList.Values.Count; i++)
        {
            if (componentsList.Values[i] is not StringTag component)
            {
                continue;
            }

            string key = component.Value;
            if (string.IsNullOrWhiteSpace(key) || _components.ContainsKey(key))
            {
                continue;
            }

            string payloadKey = key.StartsWith("minecraft:", StringComparison.Ordinal)
                ? key["minecraft:".Length..]
                : key;

            _components[key] = properties.Get<CompoundTag>(payloadKey) ?? new CompoundTag();
        }
    }

    public bool HasComponent(string identifier)
    {
        return _components.ContainsKey(identifier);
    }

    public bool TryGetComponentProperties(string identifier, out CompoundTag properties)
    {
        return _components.TryGetValue(identifier, out properties!);
    }

    public bool HasComponent<T>() where T : ItemTypeComponent
    {
        return HasComponent(GetIdentifier(typeof(T)));
    }

    public T? GetComponent<T>() where T : ItemTypeComponent
    {
        string identifier = GetIdentifier(typeof(T));
        if (!_components.TryGetValue(identifier, out CompoundTag? component))
        {
            return null;
        }

        return (T?)Activator.CreateInstance(typeof(T), _itemType, component);
    }

    private static string GetIdentifier(Type type)
    {
        if (type.GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
            property.PropertyType == typeof(string) &&
            property.GetValue(null) is string identifier &&
            !string.IsNullOrWhiteSpace(identifier))
        {
            return identifier;
        }

        throw new InvalidOperationException($"Component type {type.FullName} must declare public static string Identifier.");
    }
}






