namespace Basalt.Core.Item;

using Basalt.Core.Item.Traits;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text.Json;
using BinaryWriter = Basalt.Binary.BinaryWriter;


public sealed class ItemPalette
{
    private const string AirIdentifier = "minecraft:air";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();
    private static byte[]? _itemRegistryPayload;
    private static byte[]? _creativeContentPayload;
    private static Dictionary<uint, ItemStack>? _creativeItems;

#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
#pragma warning restore CA2255
    {
#pragma warning disable IL2026
        LoadVanilla();
        ItemTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
#pragma warning restore IL2026
    }

    public IReadOnlyDictionary<string, ItemType> Types => ItemType.Types;

    public List<ItemType> GetAllTypes()
    {
        return ItemType.GetAll();
    }

    public static byte[] GetItemRegistryPayload()
    {
        if (_itemRegistryPayload is not null)
        {
            return _itemRegistryPayload;
        }

        lock (LoadLock)
        {
            if (_itemRegistryPayload is not null)
            {
                return _itemRegistryPayload;
            }

            LoadVanilla();

            List<ItemEntry> items = [.. ItemType.GetAll().Select(type => new ItemEntry
            {
                Name = type.Identifier,
                RuntimeId = checked((short)type.NetworkId),
                ComponentBased = type.IsComponentBased,
                Version = Math.Max(2, type.Version),
                Data = type.Properties
            })];

            _itemRegistryPayload = SerializeItemRegistryBody(items);
            return _itemRegistryPayload;
        }
    }

    public static byte[] GetCreativeContentPayload()
    {
        if (_creativeContentPayload is not null)
        {
            return _creativeContentPayload;
        }

        lock (LoadLock)
        {
            if (_creativeContentPayload is not null)
            {
                return _creativeContentPayload;
            }

            LoadVanilla();

            List<ItemType> allTypes = ItemType.GetAll();
            Dictionary<string, int> groupIndexMap = new(StringComparer.Ordinal);
            List<CreativeGroup> groups = [];
            List<CreativeItem> items = [];
            Dictionary<uint, ItemStack> creativeItems = [];

            for (int i = 0; i < allTypes.Count; i++)
            {
                ItemType type = allTypes[i];
                if (type.Catalog is null || type == ItemType.Air)
                {
                    continue;
                }

                int groupIndex = ResolveGroupIndex(type.Catalog, groups, groupIndexMap);

                int blockRuntimeId = 0;
                if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
                {
                    blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
                }

                uint creativeNetworkId = checked((uint)(items.Count + 1));
                items.Add(new CreativeItem
                {
                    ItemIndex = checked((int)creativeNetworkId),
                    ItemInstance = new CreativeItemInstanceDescriptor
                    {
                        NetworkId = type.NetworkId,
                        StackSize = 1,
                        Metadata = 0,
                        NetworkBlockId = blockRuntimeId,
                        ExtraData = null
                    },
                    GroupIndex = groupIndex
                });

                creativeItems[creativeNetworkId] = new ItemStack(type, checked((ushort)type.MaxStackSize), 0, null);
            }

            CreativeContentPacket packet = new()
            {
                Groups = groups,
                Items = items
            };

            _creativeContentPayload = SerializePacketBody(packet);
            _creativeItems = creativeItems;
            return _creativeContentPayload;
        }
    }

    private static int ResolveGroupIndex(
        ItemCatalog catalog,
        List<CreativeGroup> groups,
        Dictionary<string, int> groupIndexMap)
    {
        string groupName = catalog.GroupName ?? string.Empty;
        string groupIcon = catalog.GroupIcon ?? string.Empty;
        string key = $"{catalog.Category}:{groupName}";

        if (groupIndexMap.TryGetValue(key, out int existingIndex))
        {
            return existingIndex;
        }

        int newIndex = groups.Count;
        groups.Add(new CreativeGroup
        {
            Category = catalog.Category,
            Name = groupName,
            Icon = BuildGroupIcon(groupIcon)
        });
        groupIndexMap[key] = newIndex;
        return newIndex;
    }

    public static ItemStack? GetCreativeItem(uint creativeItemNetworkId)
    {
        if (_creativeItems is null)
        {
            GetCreativeContentPayload();
        }

        return _creativeItems is not null && _creativeItems.TryGetValue(creativeItemNetworkId, out ItemStack? item)
            ? item.Clone()
            : null;
    }

    public ItemType ResolveType(string identifier)
    {
        return ItemType.GetOrAir(identifier);
    }

    public ItemType ResolveType(int networkId)
    {
        return ItemType.GetByNetwork(networkId) ?? ItemType.GetOrAir(AirIdentifier);
    }

    public static void LoadVanilla(string? dataDirectory = null)
    {
        if (_vanillaLoaded)
        {
            return;
        }

        lock (LoadLock)
        {
            if (_vanillaLoaded)
            {
                return;
            }

            string root = ResolveDataRoot(dataDirectory);
            string typesPath = Path.Combine(root, "item_types.json");
            List<ItemTypeData> types;
            using (FileStream typesStream = File.OpenRead(typesPath))
            {
                types = JsonSerializer.Deserialize(typesStream, ItemPaletteJsonContext.Default.ListItemTypeData) ?? [];
            }

            ItemType.EnsureRegistryCapacity(types.Count + 1);

            for (int i = 0; i < types.Count; i++)
            {
                ItemTypeData entry = types[i];
                if (string.IsNullOrEmpty(entry.Identifier) || entry.NetworkId is null || ItemType.Get(entry.Identifier) is not null)
                {
                    continue;
                }

                ItemCatalog? catalog = null;
                if (entry.Catalog is not null && !string.IsNullOrEmpty(entry.Catalog.CategoryName))
                {
                    string? groupName = null;
                    string? groupIcon = null;
                    if (entry.Catalog.GroupIdentifier is { } gid && !string.IsNullOrEmpty(gid.Name))
                    {
                        groupName = gid.Name;
                        groupIcon = gid.Icon;
                    }

                    catalog = new ItemCatalog(entry.Catalog.CategoryName, groupName, groupIcon);
                }

                _ = new ItemType(
                    entry.Identifier,
                    entry.NetworkId.Value,
                    entry.MaxAmount,
                    entry.Tags,
                    entry.ComponentBased,
                    entry.ItemVersion,
                    BuildProperties(entry.PropertiesPayload),
                    catalog);
            }

            _ = ItemType.Get(AirIdentifier) ?? new ItemType(AirIdentifier, 0, 64, [], true, 1);
            _vanillaLoaded = true;
        }
    }

    private static byte[] SerializeItemRegistryBody(List<ItemEntry> items)
    {
        int size = Math.Max(1024, (items.Count * 256) + 8);
        while (true)
        {
            byte[] buffer = new byte[size];
            try
            {
                int offset = 0;
                BinaryWriter writer = new(buffer, ref offset);
                writer.WriteVarUInt((uint)items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Write(writer);
                }

                return writer.GetProcessedBytes().ToArray();
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                size *= 2;
            }
        }
    }

    private static byte[] SerializePacketBody(DataPacket packet)
    {
        int size = 16384;
        while (true)
        {
            byte[] buffer = new byte[size];
            try
            {
                int offset = 0;
                BinaryWriter writer = new(buffer, ref offset);
                packet.Serialize(writer);
                return writer.GetProcessedBytes().ToArray();
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                size *= 2;
            }
        }
    }

    private static string ResolveDataRoot(string? dataDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(dataDirectory))
        {
            return dataDirectory;
        }

        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, "Protocol", "Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Protocol/Data directory.");
    }

    private static CompoundTag BuildProperties(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } element)
        {
            return new CompoundTag();
        }

        CompoundTag properties = ToCompoundTag(element);
        NormalizeItemComponents(properties);
        return properties;
    }

    private static void NormalizeItemComponents(CompoundTag properties)
    {
        if (properties.Get<ListTag>("components") is not ListTag componentList)
        {
            return;
        }

        CompoundTag components = new();
        for (int i = 0; i < componentList.Values.Count; i++)
        {
            if (componentList.Values[i] is not StringTag component || string.IsNullOrWhiteSpace(component.Value))
            {
                continue;
            }

            string identifier = component.Value;
            string payloadKey = identifier.StartsWith("minecraft:", StringComparison.Ordinal)
                ? identifier["minecraft:".Length..]
                : identifier;

            CompoundTag componentPayload = properties.Get<CompoundTag>(payloadKey) ?? new CompoundTag();
            if (identifier == "minecraft:food")
            {
                componentPayload = NormalizeFoodComponent(componentPayload);
            }

            components.Set(identifier, componentPayload);
        }

        properties.Set("components", components);
    }

    private static CompoundTag NormalizeFoodComponent(CompoundTag food)
    {
        CompoundTag normalized = new();
        normalized.Set("nutrition", new IntTag { Value = food.Get<IntTag>("nutrition")?.Value ?? 0 });
        normalized.Set("saturation_modifier", new FloatTag { Value = food.Get<FloatTag>("saturationModifier")?.Value ?? 0f });
        normalized.Set("can_always_eat", new ByteTag { Value = food.Get<ByteTag>("canAlwaysEat")?.Value ?? 0 });
        normalized.Set("using_converts_to", new StringTag { Value = food.Get<StringTag>("usingConvertsTo")?.Value ?? string.Empty });
        return normalized;
    }

    private static CompoundTag ToCompoundTag(JsonElement element)
    {
        CompoundTag tag = new();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            BaseTag? value = ToNbtTag(property.Value);
            if (value is not null)
            {
                tag.Set(property.Name, value);
            }
        }

        return tag;
    }

    private static BaseTag? ToNbtTag(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ToCompoundTag(element),
            JsonValueKind.Array => ToListTag(element),
            JsonValueKind.String => new StringTag { Value = element.GetString() ?? string.Empty },
            JsonValueKind.Number => ToNumberTag(element),
            JsonValueKind.True => new ByteTag { Value = 1 },
            JsonValueKind.False => new ByteTag { Value = 0 },
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static ListTag ToListTag(JsonElement element)
    {
        ListTag tag = new();
        foreach (JsonElement item in element.EnumerateArray())
        {
            BaseTag? value = ToNbtTag(item);
            if (value is not null)
            {
                tag.Values.Add(value);
            }
        }

        return tag;
    }

    private static BaseTag ToNumberTag(JsonElement element)
    {
        if (element.TryGetInt32(out int value))
        {
            return new IntTag { Value = value };
        }

        return new FloatTag { Value = element.GetSingle() };
    }

    private static CreativeItemInstanceDescriptor BuildGroupIcon(string identifier)
    {
        ItemType type = ItemType.Get(identifier) ?? ItemType.Air;
        int blockRuntimeId = 0;
        if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
        {
            blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
        }

        return new CreativeItemInstanceDescriptor
        {
            NetworkId = type.NetworkId,
            StackSize = 1,
            Metadata = 0,
            NetworkBlockId = blockRuntimeId,
            ExtraData = null
        };
    }
}






