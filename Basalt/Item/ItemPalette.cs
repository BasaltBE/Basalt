namespace Basalt.Core.Item;

using Basalt.Core.Item.Enchantment;
using Basalt.Core.Item.Traits;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Io;
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
        Basalt.Core.Blocks.BlockPalette.LoadVanilla();
        LoadVanilla();
        ItemTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
#pragma warning restore IL2026
    }

    public static IReadOnlyDictionary<string, ItemType> TypesMap => ItemType.Types;

    /// 
    public static void InvalidateCache()
    {
        lock (LoadLock)
        {
            _itemRegistryPayload = null;
            _creativeContentPayload = null;
            _creativeItems = null;
        }
    }

    public static List<ItemType> GetAllTypes()
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
                Version = type.Version,
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
                if (type.Catalog is null || type == ItemType.Air || type.NetworkId == 0)
                {
                    continue;
                }

                string groupName = type.Catalog.GroupName ?? string.Empty;
                string groupIcon = type.Catalog.GroupIcon ?? string.Empty;
                string key = $"{type.Catalog.Category}:{groupName}";

                int groupIndex;
                if (groupIndexMap.TryGetValue(key, out int existingIndex))
                {
                    groupIndex = existingIndex;
                }
                else
                {
                    groupIndex = groups.Count;
                    int blockRuntimeIdIcon = 0;
                    ItemType iconType = ItemType.Get(groupIcon) ?? ItemType.Air;
                    if (iconType.BlockType is not null && iconType.BlockType.Permutations.Count > 0)
                    {
                        blockRuntimeIdIcon = iconType.BlockType.Permutations[0].NetworkId;
                    }

                    groups.Add(new CreativeGroup
                    {
                        Category = type.Catalog.Category,
                        Name = groupName,
                        Icon = new LegacyNetworkItemStackDescriptor
                        {
                            NetworkId = iconType.NetworkId,
                            StackSize = 1,
                            Metadata = 0,
                            NetworkBlockId = blockRuntimeIdIcon,
                            ExtraData = null
                        }
                    });
                    groupIndexMap[key] = groupIndex;
                }

                uint creativeNetworkId = checked((uint)(items.Count + 1));

                int blockRuntimeId = 0;
                if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
                {
                    blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
                }

                items.Add(new CreativeItem
                {
                    CreativeItemNetworkId = creativeNetworkId,
                    ItemInstance = new LegacyNetworkItemStackDescriptor
                    {
                        NetworkId = type.NetworkId,
                        StackSize = 1,
                        Metadata = 0,
                        NetworkBlockId = blockRuntimeId,
                        ExtraData = null
                    },
                    GroupIndex = checked((uint)groupIndex)
                });

                creativeItems[creativeNetworkId] = new ItemStack(type, checked((ushort)type.MaxStackSize), 0, null);
            }

            AppendEnchantedBookEntries(groups, items, creativeItems, groupIndexMap);

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

    public static ItemType ResolveType(string identifier)
    {
        return ItemType.GetOrAir(identifier);
    }

    public static ItemType ResolveType(int networkId)
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

            List<ItemTypeData> types;
            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                string typesPath = Path.Combine(dataDirectory, "item_types.json");
                using FileStream typesStream = File.OpenRead(typesPath);
                types = JsonSerializer.Deserialize(typesStream, ItemPaletteJsonContext.Default.ListItemTypeData) ?? [];
            }
            else
            {
                using Stream typesStream = ProtocolData.Require("item_types.json");
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
            EnchantmentType.Load(dataDirectory);
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

    private static void AppendEnchantedBookEntries(
        List<CreativeGroup> groups,
        List<CreativeItem> items,
        Dictionary<uint, ItemStack> creativeItems,
        Dictionary<string, int> groupIndexMap)
    {
        ItemType? enchantedBook = ItemType.Get("minecraft:enchanted_book");
        if (enchantedBook is null) return;

        const int enchantedBookCategory = 4;
        const string enchantedBookGroupName = "itemGroup.name.enchantedBook";
        string key = $"{enchantedBookCategory}:{enchantedBookGroupName}";

        int groupIndex;
        if (groupIndexMap.TryGetValue(key, out int existingIndex))
        {
            groupIndex = existingIndex;
        }
        else
        {
            groupIndex = groups.Count;

            // Group icon is an enchanted book with protection I.
            CompoundTag iconNbt = Traits.ItemStackEnchantmentTrait.BuildEnchantmentNbt(
                [new Enchantment.EnchantmentInstance(EnchantmentType.Get(0)!, 1)]);

            groups.Add(new CreativeGroup
            {
                Category = enchantedBookCategory,
                Name = enchantedBookGroupName,
                Icon = new LegacyNetworkItemStackDescriptor
                {
                    NetworkId = enchantedBook.NetworkId,
                    StackSize = 1,
                    Metadata = 0,
                    NetworkBlockId = 0,
                    ExtraData = new ItemInstanceUserData
                    {
                        Nbt = iconNbt,
                        CanPlaceOn = [],
                        CanDestroy = [],
                        Ticking = null
                    }
                }
            });
            groupIndexMap[key] = groupIndex;
        }

        foreach ((int _, EnchantmentType enchantment) in EnchantmentType.All)
        {
            for (int level = 1; level <= enchantment.MaxLevel; level++)
            {
                EnchantmentInstance instance = new(enchantment, level);
                CompoundTag nbt = Traits.ItemStackEnchantmentTrait.BuildEnchantmentNbt([instance]);

                uint creativeNetworkId = checked((uint)(items.Count + 1));

                items.Add(new CreativeItem
                {
                    CreativeItemNetworkId = creativeNetworkId,
                    ItemInstance = new LegacyNetworkItemStackDescriptor
                    {
                        NetworkId = enchantedBook.NetworkId,
                        StackSize = 1,
                        Metadata = 0,
                        NetworkBlockId = 0,
                        ExtraData = new ItemInstanceUserData
                        {
                            Nbt = nbt,
                            CanPlaceOn = [],
                            CanDestroy = [],
                            Ticking = null
                        }
                    },
                    GroupIndex = checked((uint)groupIndex)
                });

                ItemStack stack = new(enchantedBook, 1, 0, new ItemInstanceUserData
                {
                    Nbt = nbt,
                    CanPlaceOn = [],
                    CanDestroy = [],
                    Ticking = null
                });
                creativeItems[creativeNetworkId] = stack;
            }
        }
    }

    private static CompoundTag BuildProperties(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } element)
        {
            return new CompoundTag();
        }

        CompoundTag properties = ToCompoundTag(element);
        SerializeComponents(properties);
        return properties;
    }

    private static void SerializeComponents(CompoundTag properties)
    {
        if (properties.Get<ListTag>("components") is not ListTag componentList)
        {
            return;
        }

        CompoundTag components = new();
        CompoundTag itemProperties = new();

        if (properties.Get<CompoundTag>("icon") is CompoundTag iconTag)
        {
            itemProperties.Set("minecraft:icon", iconTag);
        }

        if (properties.Get<IntTag>("maxAmount") is IntTag maxStack)
        {
            itemProperties.Set("max_stack_size", new IntTag { Value = maxStack.Value });
        }

        if (properties.Get<IntTag>("damage") is IntTag damage)
        {
            itemProperties.Set("damage", damage);
        }

        if (itemProperties.Values.Count > 0)
        {
            components.Set("item_properties", itemProperties);
        }

        // Resolve use_duration as a standalone component (int ticks).
        int useDurationTicks = 0;
        if (properties.Get<IntTag>("useDuration") is IntTag useDurationInt)
        {
            useDurationTicks = useDurationInt.Value;
        }
        else if (properties.Get<FloatTag>("useDuration") is FloatTag useDurationFloat)
        {
            useDurationTicks = (int)(useDurationFloat.Value * 20f);
        }

        bool hasFood = false;
        for (int i = 0; i < componentList.Values.Count; i++)
        {
            if (componentList.Values[i] is StringTag comp && comp.Value == "minecraft:food")
            {
                hasFood = true;
                break;
            }
        }

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

        // minecraft:use_duration as a standalone component (just the int value).
        if (hasFood)
        {
            int ticks = useDurationTicks > 0 ? useDurationTicks : 32;
            components.Set("minecraft:use_duration", new IntTag { Value = ticks });
        }
        else if (useDurationTicks > 0)
        {
            components.Set("minecraft:use_duration", new IntTag { Value = useDurationTicks });
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
        normalized.Set("cooldown_time", new IntTag { Value = 0 });
        normalized.Set("cooldown_type", new StringTag { Value = string.Empty });
        normalized.Set("on_use_action", new IntTag { Value = -1 });

        ListTag onUseRange = new();
        onUseRange.Values.Add(new IntTag { Value = 8 });
        onUseRange.Values.Add(new IntTag { Value = 8 });
        onUseRange.Values.Add(new IntTag { Value = 8 });
        normalized.Set("on_use_range", onUseRange);

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
}






