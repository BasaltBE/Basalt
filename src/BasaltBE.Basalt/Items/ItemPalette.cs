using System.Reflection;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items;

public static class ItemPalette {
    public static readonly ItemType[] Types = LoadTypes();
    static readonly Dictionary<string, ItemType> ByIdentifier = Types.ToDictionary(type => type.Identifier, StringComparer.Ordinal);
    static readonly Dictionary<int, ItemType> ByNetworkId = Types.ToDictionary(type => type.NetworkId);
    internal static readonly ItemRegistryPacket RegistryPacket = CreateRegistryPacket();
    internal static readonly CreativeContentPacket CreativeContentPacket = CreateCreativeContentPacket();

    public static ItemType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static ItemType? FindByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    static ItemRegistryPacket CreateRegistryPacket() {
        ItemData[] items = new ItemData[Types.Length];
        for (int i = 0; i < Types.Length; i++) {
            ItemType type = Types[i];
            items[i] = new ItemData {
                Name = type.Identifier,
                Id = checked((short)type.NetworkId),
                ComponentBased = type.ComponentBased,
                Version = type.Version,
                ComponentData = type.ComponentBased
                    ? type.PropertiesPayload.ToComponentData()
                    : new CompoundTag()
            };
        }

        return new ItemRegistryPacket { Items = items };
    }

    static CreativeContentPacket CreateCreativeContentPacket() {
        Dictionary<(CreativeItemCategory Category, string Name), int> groupIndices = [];
        List<CreativeGroupInfoPayload> groups = [];
        List<CreativeItemEntryPayload> entries = [];

        foreach (ItemType type in Types) {
            if (type.Catalog is null || type.NetworkId == 0)
                continue;

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
                int iconBlockRuntimeId = BlockPalette.FindType(iconType.Identifier) is null
                    ? 0
                    : BlockPalette.FindDefaultPermutation(iconType.Identifier)?.NetworkId ?? 0;

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

            int blockRuntimeId = BlockPalette.FindType(type.Identifier) is null
                ? 0
                : BlockPalette.FindDefaultPermutation(type.Identifier)?.NetworkId ?? 0;
            entries.Add(new CreativeItemEntryPayload {
                CreativeNetId = entries.Count + 1,
                ItemInstance = new NetworkItemInstanceDescriptor {
                    Id = type.NetworkId,
                    StackSize = 1,
                    BlockRuntimeId = blockRuntimeId
                },
                GroupIndex = checked((uint)groupIndex)
            });
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
                (ToolType.Axe, ToolMaterial.Wooden or ToolMaterial.Gold) => 6,
                (ToolType.Axe, ToolMaterial.Stone or ToolMaterial.Copper or ToolMaterial.Iron or ToolMaterial.Diamond) => 8,
                (ToolType.Axe, ToolMaterial.Netherite) => 9,
                _ => 0
            };

            type.MaxDurability = GetMaxDurability(type);
            type.AttackDurabilityDamage = type.Tags.Any(static tag =>
                tag is "minecraft:is_sword" or "minecraft:is_spear" or "minecraft:is_trident") ? 1 :
                type.Tags.Any(static tag => tag is
                    "minecraft:is_pickaxe" or "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe") ? 2 : 0;
            type.BlockDurabilityDamage = type.MaxDurability > 0 && type.Tags.Any(static tag =>
                tag is "minecraft:is_pickaxe" or "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe" or
                    "minecraft:is_sword" or "minecraft:is_spear" or "minecraft:is_trident" or "minecraft:is_shears") ? 1 : 0;

            string identifier = type.Identifier == "minecraft:fence"
                ? "minecraft:oak_fence"
                : type.Identifier;
            if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:block_placer") is CompoundTag placer &&
                placer.Get<StringTag>("block") is { } block)
                identifier = block.Value;

            type.PlacementPermutation = BlockPalette.FindDefaultPermutation(identifier);
        }
        return types;
    }

    static int GetMaxDurability(ItemType type) {
        if (!type.PropertiesPayload.Components.Contains("minecraft:durability"))
            return 0;

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
        if (specialDurability != 0)
            return specialDurability;

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
            if (pieceDurability != 0 && materialMultiplier != 0)
                return pieceDurability * materialMultiplier;
        }

        return 0;
    }
}
