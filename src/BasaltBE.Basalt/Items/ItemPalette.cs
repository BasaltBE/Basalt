using System.Reflection;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using ArmorSlot = BasaltBE.Basalt.Items.Types.ArmorSlot;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items;

public static class ItemPalette {
    public static ItemType[] Types = LoadTypes();
    static readonly Dictionary<string, ItemType> ByIdentifier = Types.ToDictionary(type => type.Identifier, StringComparer.Ordinal);
    static readonly Dictionary<int, ItemType> ByNetworkId = Types.ToDictionary(type => type.NetworkId);
    static readonly Dictionary<uint, ItemStack> ByCreativeNetId = [];
    private static readonly List<CustomItem> customItems = [];
    private static readonly HashSet<ItemType> blockItems = [];
    private static readonly Lock registryLock = new();
    private static ItemRegistryPacket? registryPacket;
    private static CreativeContentPacket? creativePacket;
    private static int nextCustomNetworkId = short.MaxValue;
    internal static bool Frozen;
    internal static ItemRegistryPacket RegistryPacket => registryPacket ??= CreateRegistryPacket();
    internal static CreativeContentPacket CreativeContentPacket => creativePacket ??= CreateCreativeContentPacket();

    internal static CustomItem Create(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        string[] parts = identifier.Split(':');
        if (parts.Length != 2 || parts.Any(part => part.Length == 0 ||
            part.Any(character => !char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character is not ('_' or '-' or '.' or '/'))) ||
            parts[0].Contains('/'))
            throw new ArgumentException("Use a namespaced item identifier, such as custom:custom_item.", nameof(identifier));

        lock (registryLock) {
            if (Frozen) throw new InvalidOperationException("Create items before starting the server.");
            if (ByIdentifier.TryGetValue(identifier, out ItemType? existing)) {
                CustomItem? registered = customItems.Find(item => ReferenceEquals(item.Type, existing));
                if (registered is not null) return registered;

                CustomItem existingDefinition = new(existing);
                customItems.Add(existingDefinition);
                Invalidate();
                return existingDefinition;
            }
            while (nextCustomNetworkId > 0 && ByNetworkId.ContainsKey(nextCustomNetworkId))
                nextCustomNetworkId--;
            if (nextCustomNetworkId == 0) throw new InvalidOperationException("No custom item network IDs remain.");

            ItemType type = new() {
                Identifier = identifier,
                NetworkId = nextCustomNetworkId--,
                ComponentBased = true,
                Version = ItemVersion.DataDriven,
                MaxAmount = 64,
                Stackable = true
            };
            CustomItem definition = new(type);
            ByIdentifier.Add(identifier, type);
            ByNetworkId.Add(type.NetworkId, type);
            Types = [.. Types, type];
            customItems.Add(definition);
            Invalidate();
            return definition;
        }
    }

    internal static void Freeze() {
        lock (registryLock) {
            foreach (CustomItem item in customItems) item.Validate();
            Invalidate();
            Frozen = true;
        }
    }

    internal static void Invalidate() {
        registryPacket = null;
        creativePacket = null;
    }

    internal static void RegisterBlockItem(ItemType type, int blockId) {
        if (Frozen) throw new InvalidOperationException("Register block items before starting the server.");
        int networkId = checked(255 - blockId);
        if (networkId < short.MinValue || ByNetworkId.ContainsKey(networkId))
            throw new InvalidOperationException($"Block item ID for '{type.Identifier}' is unavailable.");

        ByNetworkId.Remove(type.NetworkId);
        type.NetworkId = networkId;
        type.ComponentBased = false;
        type.Version = ItemVersion.None;
        ByNetworkId.Add(networkId, type);
        blockItems.Add(type);
        Invalidate();
    }

    public static ItemType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static ItemType? FindByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    public static void RegisterBlockPlacement(string itemIdentifier, string blockIdentifier) {
        ItemType item = Find(itemIdentifier)
            ?? throw new ArgumentException($"Unknown item '{itemIdentifier}'.", nameof(itemIdentifier));
        item.PlacementPermutation = BlockPalette.FindDefaultPermutation(blockIdentifier)
            ?? throw new ArgumentException($"Unknown block '{blockIdentifier}'.", nameof(blockIdentifier));
    }

    internal static ItemStack? CreateCreativeItem(uint creativeNetId) =>
        ByCreativeNetId.TryGetValue(creativeNetId, out ItemStack? item)
            ? item.Clone()
            : null;

    static ItemRegistryPacket CreateRegistryPacket() {
        HashSet<ItemType> customTypes = customItems.Select(item => item.Type).ToHashSet();
        ItemData[] items = new ItemData[Types.Length];
        for (int i = 0; i < Types.Length; i++) {
            ItemType type = Types[i];
            CompoundTag data = blockItems.Contains(type) ? new() : type.PropertiesPayload.ToComponentData();
            if (customTypes.Contains(type) && !blockItems.Contains(type)) {
                data.Add("id", new IntTag { Value = type.NetworkId });
                data.Add("name", new StringTag { Value = type.Identifier });
            }
            items[i] = new ItemData {
                Name = type.Identifier,
                Id = checked((short)type.NetworkId),
                ComponentBased = type.ComponentBased,
                Version = type.Version,
                ComponentData = data
            };
        }

        return new ItemRegistryPacket { Items = items };
    }

    static CreativeContentPacket CreateCreativeContentPacket() {
        ByCreativeNetId.Clear();
        Dictionary<(CreativeItemCategory Category, string Name), int> groupIndices = [];
        List<CreativeGroupInfoPayload> groups = [];
        List<CreativeItemEntryPayload> entries = [];

        foreach (ItemType type in Types) {
            if (type.Catalog is null || type.NetworkId == 0) continue;

            CreativeItemCategory category = type.Catalog.CategoryName switch {
                "construction" => CreativeItemCategory.Construction,
                "nature" => CreativeItemCategory.Nature,
                "equipment" => CreativeItemCategory.Equipment,
                "items" => CreativeItemCategory.Items,
                _ => throw new InvalidDataException($"Unknown creative item category '{type.Catalog.CategoryName}'.")
            };

            string groupName = type.Catalog.GroupIdentifier?.Name ?? string.Empty;
            string groupIcon = type.Catalog.GroupIdentifier?.Icon ?? string.Empty;
            (CreativeItemCategory Category, string Name) groupKey = (category, groupName);
            if (!groupIndices.TryGetValue(groupKey, out int groupIndex)) {
                ItemType iconType = Find(groupIcon) ?? Find("minecraft:air")!;
                int iconBlockRuntimeId = iconType.PlacementPermutation?.NetworkId ?? 0;

                groupIndex = groups.Count;
                groupIndices.Add(groupKey, groupIndex);
                groups.Add(new CreativeGroupInfoPayload {
                    CreativeCategory = category,
                    Name = groupName,
                    GroupIconItem = new NetworkItemInstanceDescriptor {
                        Id = iconType.NetworkId,
                        StackSize = 1,
                        BlockRuntimeId = iconBlockRuntimeId
                    }
                });
            }

            int blockRuntimeId = type.PlacementPermutation?.NetworkId ?? 0;
            entries.Add(new CreativeItemEntryPayload {
                CreativeNetId = entries.Count + 1,
                ItemInstance = new NetworkItemInstanceDescriptor {
                    Id = type.NetworkId,
                    StackSize = 1,
                    BlockRuntimeId = blockRuntimeId
                },
                GroupIndex = checked((uint)groupIndex)
            });
            ByCreativeNetId.Add(
                checked((uint)entries.Count),
                new ItemStack(type, checked((ushort)type.MaxAmount))
            );
        }

        ItemType enchantedBookType = Find("minecraft:enchanted_book")
            ?? throw new InvalidDataException("Enchanted book item is missing.");
        int enchantedBookGroup = groupIndices[(CreativeItemCategory.Items, "itemGroup.name.enchantedBook")];
        foreach (Enchantment enchantment in EnchantmentPalette.Enchantments) {
            for (int level = 1; level <= enchantment.MaximumLevel; level++) {
                ItemStack book = new(enchantedBookType);
                book.AddEnchantment(enchantment, level);
                entries.Add(new CreativeItemEntryPayload {
                    CreativeNetId = entries.Count + 1,
                    ItemInstance = new NetworkItemInstanceDescriptor {
                        Id = enchantedBookType.NetworkId,
                        StackSize = 1,
                        UserDataBuffer = book.ToNetworkStackDescriptor().UserData
                    },
                    GroupIndex = checked((uint)enchantedBookGroup)
                });
                ByCreativeNetId.Add(checked((uint)entries.Count), book);
            }
        }

        return new CreativeContentPacket {
            Groups = groups.ToArray(),
            Entries = entries.ToArray()
        };
    }

    static ItemType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.item-types.json")
            ?? throw new InvalidOperationException("Item data file 'item-types.json' is missing.");

        ItemType[] types = ItemTypeParser.Parse(stream);
        foreach (ItemType type in types) {
            ConfigureFood(type);
            foreach (string tag in type.Tags) {
                type.ToolType |= tag switch {
                    "minecraft:is_pickaxe" => ToolType.Pickaxe,
                    "minecraft:is_axe" => ToolType.Axe,
                    "minecraft:is_shovel" => ToolType.Shovel,
                    "minecraft:is_hoe" => ToolType.Hoe,
                    "minecraft:is_sword" => ToolType.Sword,
                    _ => ToolType.None
                };

                type.ToolMaterial = tag switch {
                    "minecraft:wooden_tier" => ToolMaterial.Wooden,
                    "minecraft:stone_tier" => ToolMaterial.Stone,
                    "minecraft:copper_tier" => ToolMaterial.Copper,
                    "minecraft:iron_tier" => ToolMaterial.Iron,
                    "minecraft:golden_tier" => ToolMaterial.Gold,
                    "minecraft:diamond_tier" => ToolMaterial.Diamond,
                    "minecraft:netherite_tier" => ToolMaterial.Netherite,
                    _ => type.ToolMaterial
                };
            }

            type.ToolTier = type.ToolMaterial switch {
                ToolMaterial.Stone or ToolMaterial.Copper => 1,
                ToolMaterial.Iron => 2,
                ToolMaterial.Diamond or ToolMaterial.Netherite => 3,
                _ => 0
            };
            if (type.PropertiesPayload.Values.GetValueOrDefault("item_properties") is CompoundTag itemProperties)
                type.Enchantability = itemProperties.Get<IntTag>("enchantable_value")?.Value ?? 0;
            if (type.Enchantability == 0) {
                type.Enchantability = type.ToolMaterial switch {
                    ToolMaterial.Wooden => 15,
                    ToolMaterial.Stone => 5,
                    ToolMaterial.Copper => 13,
                    ToolMaterial.Iron => 14,
                    ToolMaterial.Gold => 22,
                    ToolMaterial.Diamond => 10,
                    ToolMaterial.Netherite => 15,
                    _ => 0
                };
            }
            if (type.Enchantability == 0) {
                foreach (string tag in type.Tags) {
                    type.Enchantability = tag switch {
                        "minecraft:leather_tier" => 15,
                        "minecraft:chainmail_tier" => 12,
                        "minecraft:iron_tier" => 9,
                        "minecraft:golden_tier" => 25,
                        "minecraft:diamond_tier" => 10,
                        "minecraft:netherite_tier" => 15,
                        "minecraft:copper_tier" => 13,
                        _ => 0
                    };
                    if (type.Enchantability != 0) break;
                }
            }
            if (type.Enchantability == 0 && type.PropertiesPayload.Components.Contains("minecraft:enchantable"))
                type.Enchantability = 1;
            type.MiningSpeed = type.ToolType == ToolType.None ? 1 : type.ToolMaterial switch {
                ToolMaterial.Wooden => 2,
                ToolMaterial.Stone => 4,
                ToolMaterial.Copper => 5,
                ToolMaterial.Iron => 6,
                ToolMaterial.Gold => 12,
                ToolMaterial.Diamond => 8,
                ToolMaterial.Netherite => 9,
                _ => 1
            };
            type.AttackDamageBonus = (type.ToolType, type.ToolMaterial) switch {
                (ToolType.Sword, ToolMaterial.Wooden or ToolMaterial.Gold) => 3,
                (ToolType.Sword, ToolMaterial.Stone or ToolMaterial.Copper) => 4,
                (ToolType.Sword, ToolMaterial.Iron) => 5,
                (ToolType.Sword, ToolMaterial.Diamond) => 6,
                (ToolType.Sword, ToolMaterial.Netherite) => 7,
                (ToolType.Axe, ToolMaterial.Wooden or ToolMaterial.Gold) => 3,
                (ToolType.Axe, ToolMaterial.Stone or ToolMaterial.Copper) => 4,
                (ToolType.Axe, ToolMaterial.Iron) => 5,
                (ToolType.Axe, ToolMaterial.Diamond) => 6,
                (ToolType.Axe, ToolMaterial.Netherite) => 7,
                _ => 0
            };

            type.MaxDurability = GetMaxDurability(type);
            ConfigureArmor(type);
            type.AttackDurabilityDamage = type.Tags.Any(static tag =>
                tag is "minecraft:is_sword" or "minecraft:is_spear" or "minecraft:is_trident") ? 1 :
                type.Tags.Any(static tag => tag is
                    "minecraft:is_pickaxe" or "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe") ? 2 : 0;
            type.BlockDurabilityDamage = type.MaxDurability > 0 && type.Tags.Any(static tag =>
                tag is "minecraft:is_pickaxe" or "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe" or
                    "minecraft:is_sword" or "minecraft:is_spear" or "minecraft:is_trident" or "minecraft:is_shears") ? 1 : 0;

            string identifier = type.BlockIdentifier ?? type.Identifier;
            if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:block_placer") is CompoundTag placer &&
                placer.Get<StringTag>("block") is { } block)
                identifier = block.Value;
            type.PlacementPermutation = BlockPalette.FindDefaultPermutation(identifier) ??
                (type.LegacyName is { } legacyName ? BlockPalette.FindDefaultPermutation(legacyName) : null);
            if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:seed") is CompoundTag seed &&
                seed.Get<StringTag>("crop_result") is { } crop && crop.Value is
                    "minecraft:wheat" or "minecraft:carrots" or "minecraft:potatoes" or "minecraft:beetroot" or
                    "minecraft:melon_stem" or "minecraft:pumpkin_stem" &&
                BlockPalette.FindDefaultPermutation(crop.Value) is { } cropPermutation)
                type.SeedCropPermutation = BlockPalette.Find(crop.Value, cropPermutation.State, "growth", 0) ?? cropPermutation;
        }

        return types;
    }

    static void ConfigureFood(ItemType type) {
        if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:food") is not CompoundTag food) return;

        type.FoodNutrition = Math.Max(0, food.Get<IntTag>("nutrition")?.Value ?? 0);
        type.FoodSaturationModifier = food.Get<DoubleTag>("saturation_modifier")?.Value is double saturation &&
            double.IsFinite(saturation) && saturation > 0 && saturation <= float.MaxValue
                ? (float)saturation
                : food.Get<FloatTag>("saturation_modifier")?.Value is float floatSaturation &&
                  float.IsFinite(floatSaturation) && floatSaturation > 0
                    ? floatSaturation
                    : 0;
        type.FoodCanAlwaysEat = (food.Get<IntTag>("can_always_eat")?.Value ?? 0) != 0;

        if (type.PropertiesPayload.Values.GetValueOrDefault("item_properties") is not CompoundTag itemProperties) {
            itemProperties = new CompoundTag();
            type.PropertiesPayload.Values["item_properties"] = itemProperties;
        }

        if (!itemProperties.TryGet<IntTag>("use_animation", out _))
            itemProperties.Add("use_animation", new IntTag { Value = type.Identifier == "minecraft:honey_bottle" ? 2 : 1 });
        if (!itemProperties.TryGet<IntTag>("use_duration", out _)) {
            int duration = type.PropertiesPayload.Values.GetValueOrDefault("minecraft:use_duration") is IntTag useDuration
                ? useDuration.Value
                : 32;
            itemProperties.Add("use_duration", new IntTag { Value = duration });
        }
        type.FoodUseDurationTicks = Math.Max(1, itemProperties.Get<IntTag>("use_duration")?.Value ?? 32);
    }

    static int GetMaxDurability(ItemType type) {
        if (!type.PropertiesPayload.Components.Contains("minecraft:durability")) return 0;

        if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:durability") is CompoundTag durability) {
            if (durability.Get<IntTag>("max_durability") is { } maximum) {
                if (durability.Get<CompoundTag>("damage_chance") is { } chance) {
                    byte minimum = (byte)Math.Clamp(chance.Get<IntTag>("min")?.Value ?? 100, 0, 100);
                    byte maximumChance = (byte)Math.Clamp(chance.Get<IntTag>("max")?.Value ?? 100, 0, 100);
                    type.DurabilityDamageChanceMinimum = Math.Min(minimum, maximumChance);
                    type.DurabilityDamageChanceMaximum = Math.Max(minimum, maximumChance);
                }

                return Math.Max(0, maximum.Value);
            }
        }

        int specialDurability = type.Identifier switch {
            "minecraft:bow" => 385,
            "minecraft:crossbow" => 465,
            "minecraft:shield" => 337,
            "minecraft:fishing_rod" => 65,
            "minecraft:carrot_on_a_stick" => 26,
            "minecraft:shears" => 239,
            "minecraft:flint_and_steel" => 65,
            "minecraft:trident" => 251,
            "minecraft:turtle_helmet" => 276,
            "minecraft:elytra" => 433,
            "minecraft:warped_fungus_on_a_stick" => 101,
            "minecraft:brush" => 65,
            "minecraft:wolf_armor" => 64,
            "minecraft:mace" => 500,
            "minecraft:glow_stick" or "minecraft:sparkler" => 100,
            _ => 0
        };
        if (specialDurability != 0) return specialDurability;

        if ((type.ToolType != ToolType.None || type.Tags.Contains("minecraft:is_spear")) && type.ToolMaterial != ToolMaterial.None)
            return type.ToolMaterial switch {
                ToolMaterial.Wooden => 60,
                ToolMaterial.Stone => 132,
                ToolMaterial.Copper => 191,
                ToolMaterial.Iron => 251,
                ToolMaterial.Gold => 33,
                ToolMaterial.Diamond => 1562,
                ToolMaterial.Netherite => 2032,
                _ => 0
            };

        if (type.Tags.Contains("minecraft:is_armor")) {
            ReadOnlySpan<char> identifier = type.Identifier.AsSpan("minecraft:".Length);
            int separator = identifier.LastIndexOf('_');
            ReadOnlySpan<char> material = separator < 0 ? [] : identifier[..separator];
            ReadOnlySpan<char> piece = separator < 0 ? [] : identifier[(separator + 1)..];
            int pieceDurability = piece switch {
                "helmet" => 11,
                "chestplate" => 16,
                "leggings" => 15,
                "boots" => 13,
                _ => 0
            };
            int materialMultiplier = material switch {
                "leather" => 5,
                "chainmail" or "iron" => 15,
                "golden" => 7,
                "copper" => 11,
                "diamond" => 33,
                "netherite" => 37,
                _ => 0
            };
            if (pieceDurability != 0 && materialMultiplier != 0) return pieceDurability * materialMultiplier;
        }

        return 0;
    }

    static void ConfigureArmor(ItemType type) {
        if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:wearable") is CompoundTag wearable) {
            type.ArmorSlot = wearable.Get<StringTag>("slot")?.Value switch {
                "slot.armor.head" => ArmorSlot.Head,
                "slot.armor.chest" => ArmorSlot.Chest,
                "slot.armor.legs" => ArmorSlot.Legs,
                "slot.armor.feet" => ArmorSlot.Feet,
                _ => ReadWearableSlot(wearable)
            };
            type.ArmorProtection = wearable.Get<IntTag>("protection")?.Value ?? 0;
            if (type.ArmorSlot.HasValue) return;
        }

        if (!type.Tags.Contains("minecraft:is_armor")) return;

        ReadOnlySpan<char> identifier = type.Identifier.AsSpan("minecraft:".Length);
        type.ArmorSlot = identifier.EndsWith("_helmet") ? ArmorSlot.Head :
            identifier.EndsWith("_chestplate") || identifier.SequenceEqual("elytra") ? ArmorSlot.Chest :
            identifier.EndsWith("_leggings") ? ArmorSlot.Legs :
            identifier.EndsWith("_boots") ? ArmorSlot.Feet : null;
        if (type.ArmorSlot is not ArmorSlot slot) return;

        if (identifier.SequenceEqual("turtle_helmet")) {
            type.ArmorProtection = 2;
            return;
        }

        int materialProtection = type.Tags.Contains("minecraft:leather_tier") ? 0 :
            type.Tags.Contains("minecraft:chainmail_tier") ? 1 :
            type.Tags.Contains("minecraft:iron_tier") ? 2 :
            type.Tags.Contains("minecraft:golden_tier") ? 3 :
            type.Tags.Contains("minecraft:diamond_tier") ? 4 :
            type.Tags.Contains("minecraft:netherite_tier") ? 5 :
            type.Tags.Contains("minecraft:copper_tier") ? 6 : -1;
        type.ArmorProtection = materialProtection switch {
            0 => slot switch { ArmorSlot.Head => 1, ArmorSlot.Chest => 3, ArmorSlot.Legs => 2, _ => 1 },
            1 => slot switch { ArmorSlot.Head => 2, ArmorSlot.Chest => 5, ArmorSlot.Legs => 4, _ => 1 },
            2 => slot switch { ArmorSlot.Head => 2, ArmorSlot.Chest => 6, ArmorSlot.Legs => 5, _ => 2 },
            3 => slot switch { ArmorSlot.Head => 2, ArmorSlot.Chest => 5, ArmorSlot.Legs => 3, _ => 1 },
            4 or 5 => slot switch { ArmorSlot.Head => 3, ArmorSlot.Chest => 8, ArmorSlot.Legs => 6, _ => 3 },
            6 => slot switch { ArmorSlot.Head => 2, ArmorSlot.Chest => 5, ArmorSlot.Legs => 4, _ => 1 },
            _ => 0
        };
    }

    static ArmorSlot? ReadWearableSlot(CompoundTag wearable) => wearable.Get<IntTag>("slot")?.Value switch {
        0 => ArmorSlot.Head,
        1 => ArmorSlot.Chest,
        2 => ArmorSlot.Legs,
        3 => ArmorSlot.Feet,
        _ => null
    };
}
